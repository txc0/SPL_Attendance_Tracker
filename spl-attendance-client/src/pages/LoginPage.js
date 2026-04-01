import React, { useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { getEmployee } from '../services/api';
import Toast from '../components/Toast';

export default function LoginPage() {
  const { login } = useAuth();
  const [employeeId, setEmployeeId] = useState('');
  const [loading, setLoading]       = useState(false);
  const [toast, setToast]           = useState(null);
  const [focused, setFocused]       = useState(false);

  const showToast = (message, type = 'error') =>
    setToast({ message, type });

  const handleLogin = async (e) => {
    e.preventDefault();
    const id = parseInt(employeeId.trim(), 10);
    if (!id || id <= 0) {
      showToast('Please enter a valid Employee ID.');
      return;
    }

    setLoading(true);
    try {
      const res = await getEmployee(id);
      const emp = res.data?.data;
      if (!emp) {
        showToast('Employee not found. Please check your ID.');
        return;
      }
      if (!emp.isActive) {
        showToast('Your account is inactive. Contact your manager.');
        return;
      }
      // Successful login
      login({ id: emp.id, name: emp.name, employeeCode: emp.employeeCode });
    } catch (err) {
      if (err.response?.status === 404) {
        showToast('Employee ID not found. Please try again.');
      } else {
        showToast('Cannot connect to server. Make sure the API is running.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={styles.page}>
      {toast && (
        <Toast
          message={toast.message}
          type={toast.type}
          onClose={() => setToast(null)}
        />
      )}

      <div style={styles.card}>
        {/* Logo mark */}
        <div style={styles.logoWrap}>
          <div style={styles.logoBox}>
            <svg width="28" height="28" viewBox="0 0 28 28" fill="none">
              <rect width="28" height="28" rx="8" fill="var(--accent)"/>
              <path d="M8 14h12M14 8v12" stroke="#fff" strokeWidth="2"
                    strokeLinecap="round"/>
              <circle cx="14" cy="14" r="3" fill="#fff"/>
            </svg>
          </div>
          <span style={styles.logoText}>SPL</span>
        </div>

        <h1 style={styles.title}>Welcome back</h1>
        <p style={styles.subtitle}>Sign in with your Employee ID to continue</p>

        <form onSubmit={handleLogin} style={styles.form}>
          <div style={styles.fieldWrap}>
            <label style={styles.label}>Employee ID</label>
            <input
              type="number"
              value={employeeId}
              onChange={e => setEmployeeId(e.target.value)}
              onFocus={() => setFocused(true)}
              onBlur={() => setFocused(false)}
              placeholder="e.g. 1"
              style={{
                ...styles.input,
                borderColor: focused ? 'var(--accent)' : 'var(--border)',
                boxShadow:   focused ? '0 0 0 3px rgba(26,107,74,0.12)' : 'none',
              }}
              disabled={loading}
              autoFocus
            />
          </div>

          <button
            type="submit"
            disabled={loading || !employeeId}
            style={{
              ...styles.btn,
              opacity: loading || !employeeId ? 0.6 : 1,
              cursor:  loading || !employeeId ? 'not-allowed' : 'pointer',
            }}
          >
            {loading ? (
              <span style={{ display: 'flex', alignItems: 'center', gap: '8px', justifyContent: 'center' }}>
                <Spinner /> Signing in...
              </span>
            ) : (
              'Sign In'
            )}
          </button>
        </form>

        <p style={styles.hint}>
          Enter the Employee ID assigned by your manager
        </p>
      </div>

      {/* Decorative background dots */}
      <div style={styles.dots} aria-hidden="true">
        {Array.from({ length: 80 }).map((_, i) => (
          <div key={i} style={styles.dot} />
        ))}
      </div>
    </div>
  );
}

function Spinner() {
  return (
    <svg width="16" height="16" viewBox="0 0 16 16" style={{ animation: 'spin 0.8s linear infinite' }}>
      <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>
      <circle cx="8" cy="8" r="6" fill="none" stroke="currentColor"
              strokeWidth="2" strokeDasharray="20 18" strokeLinecap="round"/>
    </svg>
  );
}

const styles = {
  page: {
    minHeight:      '100vh',
    display:        'flex',
    alignItems:     'center',
    justifyContent: 'center',
    padding:        '24px',
    position:       'relative',
    overflow:       'hidden',
    background:     'var(--bg)',
  },
  dots: {
    position:       'absolute',
    inset:          0,
    display:        'grid',
    gridTemplateColumns: 'repeat(10, 1fr)',
    gap:            '32px',
    padding:        '32px',
    pointerEvents:  'none',
    zIndex:         0,
  },
  dot: {
    width:        '4px',
    height:       '4px',
    borderRadius: '50%',
    background:   'var(--border)',
    alignSelf:    'center',
    justifySelf:  'center',
  },
  card: {
    position:     'relative',
    zIndex:       1,
    background:   'var(--surface)',
    border:       '1px solid var(--border)',
    borderRadius: '20px',
    padding:      '48px 44px',
    width:        '100%',
    maxWidth:     '400px',
    boxShadow:    'var(--shadow)',
  },
  logoWrap: {
    display:     'flex',
    alignItems:  'center',
    gap:         '10px',
    marginBottom:'32px',
  },
  logoBox: {
    display: 'flex',
  },
  logoText: {
    fontSize:   '18px',
    fontWeight: '600',
    color:      'var(--text-pri)',
    letterSpacing: '0.04em',
  },
  title: {
    fontSize:     '24px',
    fontWeight:   '600',
    color:        'var(--text-pri)',
    marginBottom: '6px',
    letterSpacing: '-0.3px',
  },
  subtitle: {
    fontSize:     '14px',
    color:        'var(--text-sec)',
    marginBottom: '32px',
    lineHeight:   '1.5',
  },
  form: {
    display:       'flex',
    flexDirection: 'column',
    gap:           '20px',
  },
  fieldWrap: {
    display:       'flex',
    flexDirection: 'column',
    gap:           '6px',
  },
  label: {
    fontSize:   '13px',
    fontWeight: '500',
    color:      'var(--text-sec)',
  },
  input: {
    width:        '100%',
    padding:      '11px 14px',
    fontSize:     '15px',
    color:        'var(--text-pri)',
    background:   'var(--bg)',
    border:       '1.5px solid var(--border)',
    borderRadius: '10px',
    transition:   'border-color 0.15s, box-shadow 0.15s',
    appearance:   'textfield',
    MozAppearance: 'textfield',
  },
  btn: {
    width:        '100%',
    padding:      '12px',
    fontSize:     '15px',
    fontWeight:   '500',
    color:        '#fff',
    background:   'var(--accent)',
    borderRadius: '10px',
    transition:   'background 0.15s, opacity 0.15s',
    letterSpacing: '0.01em',
  },
  hint: {
    marginTop:  '20px',
    fontSize:   '12px',
    color:      'var(--text-hint)',
    textAlign:  'center',
    lineHeight: '1.5',
  },
};
