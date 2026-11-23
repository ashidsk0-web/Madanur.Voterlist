using System.Data;
using Microsoft.Data.Sqlite;

namespace MadanuruVoterSeva.Backend.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
