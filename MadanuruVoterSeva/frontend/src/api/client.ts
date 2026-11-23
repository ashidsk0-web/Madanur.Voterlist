import axios from 'axios';
import type {
  DashboardStatistics,
  VoterSearchResult,
  VoterDetail,
  WardSummary,
  PagedResult,
  ImportPreview,
  ImportValidationSummary
} from '../types';

// In development, Vite server proxies /api to backend port 5000 (or 5286)
export const apiClient = axios.create({
  baseURL: '/api',
  timeout: 60000,
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('madanuru_auth_token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

function clearSession() {
  for (const key of ['auth_token', 'username', 'role']) {
    sessionStorage.removeItem('madanuru_' + key);
    localStorage.removeItem('madanuru_' + key);
  }
}

apiClient.interceptors.response.use(response => response, error => {
  if (error.response?.status === 401 && error.config?.url !== '/auth/login') {
    clearSession();
    window.dispatchEvent(new Event('madanuru-session-expired'));
  }
  return Promise.reject(error);
});

export const api = {
  // Dashboard
  getStatistics: async (): Promise<DashboardStatistics> => {
    const res = await apiClient.get<DashboardStatistics>('/dashboard/statistics');
    return res.data;
  },

  // Search
  searchByEpic: async (epicNumber: string): Promise<VoterSearchResult> => {
    const res = await apiClient.get<VoterSearchResult>('/voters/search/epic', {
      params: { epicNumber: epicNumber.trim() },
    });
    return res.data;
  },

  searchByName: async (
    name?: string,
    wardNumber?: number,
    page = 1,
    pageSize = 15,
    signal?: AbortSignal
  ): Promise<PagedResult<VoterSearchResult>> => {
    const res = await apiClient.get<PagedResult<VoterSearchResult>>('/voters/search/name', {
      signal,
      params: {
        name: name ? name.trim() : undefined,
        wardNumber: wardNumber && wardNumber > 0 ? wardNumber : undefined,
        page,
        pageSize,
      },
    });
    return res.data;
  },

  getVoterById: async (id: number, signal?: AbortSignal): Promise<VoterDetail> => {
    const res = await apiClient.get<VoterDetail>(`/voters/${id}`, { signal });
    return res.data;
  },

  getVoterPhoto: async (id: number, signal: AbortSignal): Promise<Blob> => {
    const res = await apiClient.get<Blob>(`/voters/${id}/photo`, { responseType: 'blob', signal });
    return res.data;
  },

  getVoterSourceLink: async (id: number) => {
    const res = await apiClient.get<{ originalPdfAvailable: boolean; originalPdfPath: string | null }>(`/voters/${id}/source`);
    if (!res.data.originalPdfAvailable || !res.data.originalPdfPath) throw new Error('PDF unavailable');
    return { url: res.data.originalPdfPath };
  },

  // Wards
  getWards: async (): Promise<WardSummary[]> => {
    const res = await apiClient.get<WardSummary[]>('/wards');
    return res.data;
  },

  getVotersByWard: async (
    wardId: number,
    page = 1,
    pageSize = 20
  ): Promise<PagedResult<VoterSearchResult>> => {
    const res = await apiClient.get<PagedResult<VoterSearchResult>>(`/wards/${wardId}/voters`, {
      params: { page, pageSize },
    });
    return res.data;
  },

  // Import & Roll Management
  getImportPreview: async (): Promise<ImportPreview> => {
    const res = await apiClient.get<ImportPreview>('/import/preview');
    return res.data;
  },

  extractRoll: async (): Promise<{ message: string; rollVersionId: number; validation: ImportValidationSummary }> => {
    const res = await apiClient.post('/import/extract');
    return res.data;
  },

  validateRoll: async (rollVersionId?: number): Promise<ImportValidationSummary> => {
    const res = await apiClient.post<ImportValidationSummary>('/import/validate', null, {
      params: { rollVersionId },
    });
    return res.data;
  },

  approveRoll: async (rollVersionId?: number): Promise<{ message: string; success: boolean }> => {
    const res = await apiClient.post('/import/approve', null, {
      params: { rollVersionId },
    });
    return res.data;
  },

  getImportStatus: async () => {
    const res = await apiClient.get('/import/status');
    return res.data;
  },

  // Auth
  login: async (credentials: { username: string; password: string }) => {
    const res = await apiClient.post<{ token: string; username: string; role: string }>('/auth/login', credentials);
    if (res.data.token) {
      sessionStorage.setItem('madanuru_auth_token', res.data.token);
      sessionStorage.setItem('madanuru_username', res.data.username);
      sessionStorage.setItem('madanuru_role', res.data.role);
    }
    return res.data;
  },

  logout: async () => {
    await apiClient.post('/auth/logout');
    clearSession();
  },

  getCurrentUser: () => {
    const token = sessionStorage.getItem('madanuru_auth_token');
    const username = sessionStorage.getItem('madanuru_username');
    const role = sessionStorage.getItem('madanuru_role');
    return {
      isAuthenticated: !!token,
      username: username || '',
      role: role || '',
    };
  },
};
