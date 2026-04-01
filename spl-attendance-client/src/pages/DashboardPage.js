import React, { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../context/AuthContext';
import { checkIn, checkOut, getAttendanceByDate, getAttendanceLogs } from '../services/api';
import Toast from '../components/Toast';

function getTodayStr() {
  return new Date().toISOString().split('T')[0]; // yyyy-MM-dd
}

function formatTime(dt) {
  if (!dt) return '—';
  return new Date(dt).toLocaleTimeString('en-US', {
    hour:   '2-digit',
    minute: '2-digit',
    hour12: true,
  });
}

function formatDate() {
  return new Date().toLocaleDateString('en-US', {
    weekday: 'long',
    year:    'numeric',
    month:   'long',
    day:     'numeric',
  });
}

export default function DashboardPage() {
  const { user, logout } = useAuth();

  const [todaySummary, setTodaySummary] = useState(null);
  const [logs, setLogs]                 = useState([]);
  const [loading, setLoading]           = useState(false);
  const [fetching, setFetching]         = useState(true);
  const [toast, setToast]               = useState(null);

  const showToast = (message, type = 'success') =>
    setToast({ message, type });

  // ── Fetch today's status ──────────────────────────────────
  const fetchTodayStatus = useCallback(async () => {
    try {
      const today = getTodayStr();
      const [summaryRes, logsRes] = await Promise.allSettled([
        getAttendanceByDate(user.id, today),
        getAttendanceLogs(user.id),
      ]);

      if (summaryRes.status === 'fulfilled') {
        setTodaySummary(summaryRes.value.data?.data || null);
      } else {
        setTodaySummary(null);
      }

      if (logsRes.status === 'fulfilled') {
        const allLogs = logsRes.value.data?.data || [];
        // Only show today's logs
        const todayLogs = allLogs.filter(l =>
          l.logDate?.startsWith(today)
        );
        setLogs(todayLogs);
      }
    } catch {
      // No record yet — that is fine
    } finally {
      setFetching(false);
    }
  }, [user.id]);

  useEffect(() => {
    fetchTodayStatus();
  }, [fetchTodayStatus]);

  // ── Determine current state ───────────────────────────────
  // hasOpenLog = last log has checkIn but no checkOut
  const hasOpenLog = logs.some(l => l.checkInTime && !l.checkOutTime);
  const hasCheckedInToday = logs.length > 0;

  // ── Handlers ──────────────────────────────────────────────
  const handleCheckIn = async () => {
    setLoading(true);
    try {
      await checkIn(user.id);
      showToast('✓ Checked in successfully!', 'success');
      await fetchTodayStatus();
    } catch (err) {
      const msg = err.response?.data?.message || 'Check-in failed. Please try again.';
      showToast(msg, 'error');
    } finally {
      setLoading(false);
    }
  };

  const handleCheckOut = async () => {
    setLoading(true);
    try {
      await checkOut(user.id);
      showToast('✓ Checked out successfully!', 'success');
      await fetchTodayStatus();
    } catch (err) {
      const msg = err.response?.data?.message || 'Check-out failed. Please try again.';
      showToast(msg, 'error');
    } finally {
      setLoading(false);
    }
  };

  // ── Live clock ────────────────────────────────────────────
  const [clock, setClock] = useState(
    new Date().toLocaleTimeString('en-US', {
      hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true,
    })
  );
  useEffect(() => {
    const t = setInterval(() => {
      setClock(new Date().toLocaleTimeString('en-US', {
        hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true,
      }));
    }, 1000);
    return () => clearInterval(t);
  }, []);

  // ── Status label ──────────────────────────────────────────
  const statusLabel = hasOpenLog
    ? { text: 'Currently Working', color: 'var(--accent)',  bg: 'var(--accent-lt)' }
    : hasCheckedInToday
    ? { text: 'Day Complete',       color: '#7B5EA7',       bg: '#F3EEFF' }
    : { text: 'Not Checked In',     color: 'var(--text-sec)', bg: 'var(--border)' };

  return (
    <div style={styles.page}>
      {toast && (
        <Toast
          message={toast.message}
          type={toast.type}
          onClose={() => setToast(null)}
        />
      )}

      {/* ── Header ── */}
      <header style={styles.header}>
        <div style={styles.headerLeft}>
          <div style={styles.logoMark}>
            <svg width="22" height="22" viewBox="0 0 28 28" fill="none">
              <rect width="28" height="28" rx="8" fill="var(--accent)"/>
              <path d="M8 14h12M14 8v12" stroke="#fff" strokeWidth="2" strokeLinecap="round"/>
              <circle cx="14" cy="14" r="3" fill="#fff"/>
            </svg>
          </div>
          <span style={styles.logoLabel}>SPL Attendance</span>
        </div>
        <div style={styles.headerRight}>
          <span style={styles.empCode}>{user.employeeCode}</span>
          <button onClick={logout} style={styles.logoutBtn}>Sign out</button>
        </div>
      </header>

      <main style={styles.main}>
        {/* ── Hero card ── */}
        <div style={styles.heroCard}>
          {/* Greeting */}
          <div style={styles.greeting}>
            <p style={styles.dateStr}>{formatDate()}</p>
            <h2 style={styles.name}>Good {getGreeting()}, {user.name.split(' ')[0]}</h2>
          </div>

          {/* Clock */}
          <div style={styles.clockWrap}>
            <span style={styles.clock}>{clock}</span>
          </div>

          {/* Status pill */}
          <div style={{ display: 'flex', justifyContent: 'center', marginBottom: '32px' }}>
            <span style={{
              ...styles.statusPill,
              color:      statusLabel.color,
              background: statusLabel.bg,
            }}>
              <span style={{
                width: '6px', height: '6px', borderRadius: '50%',
                background: statusLabel.color, display: 'inline-block',
                marginRight: '6px',
                animation: hasOpenLog ? 'pulse 1.5s infinite' : 'none',
              }}/>
              {statusLabel.text}
            </span>
          </div>
          <style>{`
            @keyframes pulse {
              0%, 100% { opacity: 1; }
              50% { opacity: 0.4; }
            }
          `}</style>

          {/* Action buttons */}
          {fetching ? (
            <div style={styles.loadingRow}>
              <div style={styles.skeletonBtn}/>
            </div>
          ) : (
            <div style={styles.btnRow}>
              {/* Check-In button — always shown */}
              <button
                onClick={handleCheckIn}
                disabled={loading || hasOpenLog}
                style={{
                  ...styles.actionBtn,
                  ...styles.checkInBtn,
                  opacity: loading || hasOpenLog ? 0.45 : 1,
                  cursor:  loading || hasOpenLog ? 'not-allowed' : 'pointer',
                }}
              >
                <BtnIcon type="in" />
                Check In
              </button>

              {/* Check-Out button — shown after first check-in */}
              {hasCheckedInToday && (
                <button
                  onClick={handleCheckOut}
                  disabled={loading || !hasOpenLog}
                  style={{
                    ...styles.actionBtn,
                    ...styles.checkOutBtn,
                    opacity: loading || !hasOpenLog ? 0.45 : 1,
                    cursor:  loading || !hasOpenLog ? 'not-allowed' : 'pointer',
                  }}
                >
                  <BtnIcon type="out" />
                  Check Out
                </button>
              )}
            </div>
          )}

          {/* Today summary row */}
          {todaySummary && (
            <div style={styles.summaryRow}>
              <SummaryItem label="First In"    value={formatTime(todaySummary.checkInTime)} />
              <SummaryDivider />
              <SummaryItem label="Last Out"    value={formatTime(todaySummary.checkOutTime)} />
              <SummaryDivider />
              <SummaryItem label="Work Hours"  value={todaySummary.workHours != null ? `${todaySummary.workHours}h` : '—'} />
            </div>
          )}
        </div>

        {/* ── Today's log ── */}
        {logs.length > 0 && (
          <div style={styles.logCard}>
            <h3 style={styles.logTitle}>Today's Activity</h3>
            <div style={styles.logList}>
              {logs.map((log, i) => (
                <div key={log.id} style={styles.logRow}>
                  <div style={{
                    ...styles.logDot,
                    background: log.checkOutTime ? '#7B5EA7' : 'var(--accent)',
                  }}/>
                  <div style={styles.logInfo}>
                    <span style={styles.logLabel}>
                      Session {i + 1}
                    </span>
                    <span style={styles.logTimes}>
                      {formatTime(log.checkInTime)}
                      {log.checkOutTime && ` → ${formatTime(log.checkOutTime)}`}
                      {!log.checkOutTime && (
                        <span style={{ color: 'var(--accent)', marginLeft: '6px', fontSize: '11px' }}>
                          active
                        </span>
                      )}
                    </span>
                  </div>
                  {log.checkInTime && log.checkOutTime && (
                    <span style={styles.logDuration}>
                      {calcDuration(log.checkInTime, log.checkOutTime)}
                    </span>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

// ── Helpers ───────────────────────────────────────────────────────────────

function getGreeting() {
  const h = new Date().getHours();
  if (h < 12) return 'morning';
  if (h < 17) return 'afternoon';
  return 'evening';
}

function calcDuration(start, end) {
  const ms  = new Date(end) - new Date(start);
  const h   = Math.floor(ms / 3600000);
  const m   = Math.floor((ms % 3600000) / 60000);
  if (h > 0) return `${h}h ${m}m`;
  return `${m}m`;
}

function SummaryItem({ label, value }) {
  return (
    <div style={{ textAlign: 'center', flex: 1 }}>
      <p style={{ fontSize: '11px', color: 'var(--text-hint)', marginBottom: '3px', textTransform: 'uppercase', letterSpacing: '0.06em' }}>{label}</p>
      <p style={{ fontSize: '16px', fontWeight: '600', color: 'var(--text-pri)', fontFamily: "'DM Mono', monospace" }}>{value}</p>
    </div>
  );
}

function SummaryDivider() {
  return <div style={{ width: '1px', background: 'var(--border)', alignSelf: 'stretch', margin: '0 4px' }} />;
}

function BtnIcon({ type }) {
  if (type === 'in') return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none">
      <circle cx="9" cy="9" r="8" stroke="currentColor" strokeWidth="1.5"/>
      <path d="M9 5v8M6 10l3 3 3-3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  );
  return (
    <svg width="18" height="18" viewBox="0 0 18 18" fill="none">
      <circle cx="9" cy="9" r="8" stroke="currentColor" strokeWidth="1.5"/>
      <path d="M9 13V5M6 8l3-3 3 3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  );
}

// ── Styles ────────────────────────────────────────────────────────────────

const styles = {
  page: {
    minHeight:  '100vh',
    background: 'var(--bg)',
    display:    'flex',
    flexDirection: 'column',
  },
  header: {
    display:         'flex',
    alignItems:      'center',
    justifyContent:  'space-between',
    padding:         '16px 24px',
    background:      'var(--surface)',
    borderBottom:    '1px solid var(--border)',
    position:        'sticky',
    top:             0,
    zIndex:          100,
  },
  headerLeft: {
    display:    'flex',
    alignItems: 'center',
    gap:        '10px',
  },
  logoMark: { display: 'flex' },
  logoLabel: {
    fontSize:      '15px',
    fontWeight:    '600',
    color:         'var(--text-pri)',
    letterSpacing: '-0.2px',
  },
  headerRight: {
    display:    'flex',
    alignItems: 'center',
    gap:        '12px',
  },
  empCode: {
    fontSize:     '12px',
    fontFamily:   "'DM Mono', monospace",
    color:        'var(--text-sec)',
    background:   'var(--bg)',
    border:       '1px solid var(--border)',
    borderRadius: '6px',
    padding:      '3px 8px',
  },
  logoutBtn: {
    fontSize:     '13px',
    color:        'var(--text-sec)',
    background:   'none',
    border:       '1px solid var(--border)',
    borderRadius: '8px',
    padding:      '5px 12px',
    transition:   'all 0.15s',
  },
  main: {
    flex:          1,
    maxWidth:      '520px',
    margin:        '0 auto',
    width:         '100%',
    padding:       '32px 20px 60px',
    display:       'flex',
    flexDirection: 'column',
    gap:           '16px',
  },
  heroCard: {
    background:   'var(--surface)',
    border:       '1px solid var(--border)',
    borderRadius: '20px',
    padding:      '36px 32px 28px',
    boxShadow:    'var(--shadow)',
  },
  greeting: {
    marginBottom: '20px',
  },
  dateStr: {
    fontSize:     '12px',
    color:        'var(--text-hint)',
    marginBottom: '4px',
    letterSpacing: '0.02em',
  },
  name: {
    fontSize:      '22px',
    fontWeight:    '600',
    color:         'var(--text-pri)',
    letterSpacing: '-0.3px',
  },
  clockWrap: {
    textAlign:    'center',
    marginBottom: '20px',
  },
  clock: {
    fontSize:    '40px',
    fontWeight:  '300',
    fontFamily:  "'DM Mono', monospace",
    color:       'var(--text-pri)',
    letterSpacing: '-1px',
  },
  statusPill: {
    display:      'inline-flex',
    alignItems:   'center',
    fontSize:     '13px',
    fontWeight:   '500',
    padding:      '5px 12px',
    borderRadius: '20px',
  },
  loadingRow: {
    display:        'flex',
    justifyContent: 'center',
    marginBottom:   '24px',
  },
  skeletonBtn: {
    width:        '160px',
    height:       '48px',
    borderRadius: '12px',
    background:   'var(--border)',
    animation:    'shimmer 1.2s infinite',
  },
  btnRow: {
    display:        'flex',
    gap:            '12px',
    justifyContent: 'center',
    marginBottom:   '28px',
    flexWrap:       'wrap',
  },
  actionBtn: {
    display:      'flex',
    alignItems:   'center',
    gap:          '8px',
    padding:      '13px 28px',
    borderRadius: '12px',
    fontSize:     '15px',
    fontWeight:   '500',
    transition:   'opacity 0.15s, transform 0.1s',
    minWidth:     '140px',
    justifyContent: 'center',
  },
  checkInBtn: {
    background: 'var(--accent)',
    color:      '#fff',
  },
  checkOutBtn: {
    background: 'var(--bg)',
    color:      'var(--text-pri)',
    border:     '1.5px solid var(--border)',
  },
  summaryRow: {
    display:       'flex',
    alignItems:    'center',
    background:    'var(--bg)',
    borderRadius:  '12px',
    padding:       '14px 16px',
    marginTop:     '4px',
  },
  logCard: {
    background:   'var(--surface)',
    border:       '1px solid var(--border)',
    borderRadius: '16px',
    padding:      '24px',
    boxShadow:    'var(--shadow)',
  },
  logTitle: {
    fontSize:     '14px',
    fontWeight:   '600',
    color:        'var(--text-sec)',
    marginBottom: '16px',
    textTransform: 'uppercase',
    letterSpacing: '0.06em',
  },
  logList: {
    display:       'flex',
    flexDirection: 'column',
    gap:           '12px',
  },
  logRow: {
    display:    'flex',
    alignItems: 'center',
    gap:        '12px',
  },
  logDot: {
    width:        '8px',
    height:       '8px',
    borderRadius: '50%',
    flexShrink:   0,
  },
  logInfo: {
    flex:          1,
    display:       'flex',
    flexDirection: 'column',
    gap:           '2px',
  },
  logLabel: {
    fontSize:   '12px',
    color:      'var(--text-hint)',
    fontWeight: '500',
  },
  logTimes: {
    fontSize:   '14px',
    color:      'var(--text-pri)',
    fontFamily: "'DM Mono', monospace",
    fontWeight: '500',
  },
  logDuration: {
    fontSize:     '12px',
    color:        'var(--text-sec)',
    background:   'var(--bg)',
    border:       '1px solid var(--border)',
    borderRadius: '6px',
    padding:      '2px 8px',
    fontFamily:   "'DM Mono', monospace",
  },
};
