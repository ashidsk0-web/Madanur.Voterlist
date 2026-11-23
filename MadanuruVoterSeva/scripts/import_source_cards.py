"""Backfill source cards and original portraits for the active Madanuru roll."""

import argparse
import hashlib
import io
from pathlib import Path
import re
import sqlite3
from contextlib import closing

from PIL import Image
import pdfplumber
import pypdfium2 as pdfium
from pypdfium2.raw import FPDF_PAGEOBJ_IMAGE

EPIC = re.compile(r"[A-Z]{2,4}\d{6,8}")
NUMBER = re.compile(r"\d+")
AGE = re.compile(r":\s*(\d{1,3})\b")
CREATE = """
CREATE TABLE IF NOT EXISTS VoterSourceCard (
    VoterId INTEGER PRIMARY KEY REFERENCES Voter(VoterId) ON DELETE CASCADE,
    PdfSha256 TEXT NOT NULL,
    SourcePageNumber INTEGER NOT NULL,
    RawCardText TEXT NOT NULL,
    Age INTEGER,
    RelativeLabel TEXT,
    AssemblyConstituencyNumber INTEGER,
    PhotoStatus TEXT NOT NULL CHECK (PhotoStatus IN ('Embedded', 'Missing', 'Ambiguous')),
    PhotoData BLOB,
    PhotoMimeType TEXT,
    PhotoWidth INTEGER,
    PhotoHeight INTEGER,
    CHECK ((PhotoStatus = 'Embedded' AND PhotoData IS NOT NULL AND PhotoMimeType IS NOT NULL
            AND PhotoWidth > 0 AND PhotoHeight > 0)
           OR (PhotoStatus <> 'Embedded' AND PhotoData IS NULL AND PhotoMimeType IS NULL
               AND PhotoWidth IS NULL AND PhotoHeight IS NULL))
)
"""


def extract_photo(image):
    buffer = io.BytesIO()
    image.extract(buffer, fb_format="png")
    with Image.open(buffer) as opened:
        mime = Image.MIME[opened.format]
        dimensions = opened.size
        opened.load()
    return buffer.getvalue(), mime, *dimensions


def read_relative_label(lines, age_line):
    fields = [line for line in lines[:age_line] if ":" in line]
    if len(fields) < 2:
        return None
    return fields[-1].split(":", 1)[0].strip() or None


def import_cards(connection, pdf_path, checksum):
    roll, = connection.execute("SELECT RollVersionId FROM RollVersion WHERE IsActive = 1").fetchone()
    voters = connection.execute("""
        SELECT VoterId, NormalizedEPIC, SourcePageNumber, GramPanchayatSerialNumber
        FROM Voter WHERE RollVersionId = ?
    """, (roll,)).fetchall()
    by_page = {}
    for voter_id, epic, page, serial in voters:
        records = by_page.setdefault(page, {})
        if epic in records:
            raise ValueError(f"Duplicate voter ID on source page {page}")
        records[epic] = (voter_id, serial)

    if len(voters) != 5543:
        raise ValueError(f"Expected 5543 active voter rows, found {len(voters)}")

    connection.execute(CREATE)
    insert = """
        INSERT OR REPLACE INTO VoterSourceCard (
            VoterId, PdfSha256, SourcePageNumber, RawCardText, Age, RelativeLabel,
            AssemblyConstituencyNumber, PhotoStatus, PhotoData, PhotoMimeType, PhotoWidth, PhotoHeight
        ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
    """
    counts = dict.fromkeys(("Embedded", "Missing", "Ambiguous", "Age missing", "Assembly missing", "Relative label missing"), 0)
    with pdfplumber.open(pdf_path) as text_pdf, pdfium.PdfDocument(pdf_path) as image_pdf:
        for number, records in sorted(by_page.items()):
            page = text_pdf.pages[number - 1]
            words = page.extract_words()
            markers = [word for word in words if EPIC.fullmatch(word["text"]) and word["text"] in records]
            if len(markers) != len(records) or {word["text"] for word in markers} != records.keys():
                raise ValueError(f"Voter ID reconciliation failed on page {number}")

            with closing(image_pdf[number - 1]) as image_page:
                images = [(image, image.get_bounds()) for image in image_page.get_objects(filter=[FPDF_PAGEOBJ_IMAGE])]
                used_images = set()
                for word in markers:
                    voter_id, serial = records[word["text"]]
                    x, y = word["x0"], word["top"]
                    region = page.crop((x - 67, y - 66, x + 110, word["bottom"] + 4))
                    text = region.extract_text()
                    lines = text.splitlines()
                    header = NUMBER.findall(text)
                    serial_digits = str(serial)
                    prefix = ""
                    while header and len(prefix) < len(serial_digits):
                        prefix += header.pop(0)
                    if prefix != serial_digits or word["text"] not in text:
                        raise ValueError(f"Card verification failed on page {number}, serial {serial}")
                    age_line = next((i for i, line in enumerate(lines) if re.search(r":\s*\d{1,3}\s+[^:\d]+:", line)), None)
                    age = AGE.search(lines[age_line]) if age_line is not None else None
                    relative = read_relative_label(lines, age_line)

                    candidates = [
                        index for index, (_, (left, bottom, right, top)) in enumerate(images)
                        if 61 <= left - x <= 72 and 35 <= right - left <= 45
                        and 35 <= top - bottom <= 45
                        and -2 <= page.height - bottom - y <= 9
                    ]
                    photo = (None, None, None, None)
                    status = "Missing"
                    if len(candidates) > 1:
                        status = "Ambiguous"
                    elif candidates:
                        index = candidates[0]
                        if index in used_images:
                            raise ValueError(f"An image matched multiple cards on page {number}")
                        used_images.add(index)
                        photo = extract_photo(images[index][0])
                        status = "Embedded"
                    connection.execute(insert, (
                        voter_id, checksum, number, text, int(age.group(1)) if age else None,
                        relative, int(header[0]) if len(header) >= 3 else None, status, *photo
                    ))
                    counts[status] += 1
                    counts["Age missing"] += age is None
                    counts["Assembly missing"] += len(header) < 3
                    counts["Relative label missing"] += relative is None
            page.close()
            if number % 25 == 0:
                print(f"Processed through source page {number}", flush=True)

    if sum(counts[key] for key in ("Embedded", "Missing", "Ambiguous")) != len(voters):
        raise ValueError("Source card count differs from the active roll")
    return counts


def main():
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--pdf", type=Path, default=root / "data/source/MADANURU WITH SORTING VOTER.pdf")
    parser.add_argument("--database", type=Path, default=root / "data/database/madanuru_voter_seva.db")
    args = parser.parse_args()
    with args.pdf.open("rb") as source:
        checksum = hashlib.file_digest(source, "sha256").hexdigest()
    with closing(sqlite3.connect(args.database.resolve().as_uri() + "?mode=rw", uri=True)) as connection:
        connection.execute("PRAGMA foreign_keys = ON")
        with connection:
            connection.execute("BEGIN IMMEDIATE")
            counts = import_cards(connection, args.pdf, checksum)
        print(counts)


if __name__ == "__main__":
    main()





