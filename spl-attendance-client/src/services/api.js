import axios from 'axios';

// Change this to match your actual API URL
const API_BASE = 'https://localhost:7001/api';

const api = axios.create({
  baseURL: API_BASE,
  headers: { 'Content-Type': 'application/json' },
  // Required for self-signed certs in development
  httpsAgent: undefined,
});

// ── Employee ────────────────────────────────────────────────
export const getEmployee = (id) =>
  api.get(`/employees/${id}`);

export const getAllEmployees = () =>
  api.get('/employees');

// ── Attendance ──────────────────────────────────────────────
export const checkIn = (employeeId) =>
  api.post('/attendance/checkin', { employeeId });

export const checkOut = (employeeId) =>
  api.post('/attendance/checkout', { employeeId });

export const getAttendanceByDate = (employeeId, date) =>
  api.get(`/attendance/${employeeId}/${date}`);

export const getAttendanceLogs = (employeeId) =>
  api.get(`/attendance/${employeeId}/logs`);

export default api;
