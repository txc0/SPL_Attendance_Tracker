import React, { useEffect } from 'react';

const icons = {
  success: (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
      <circle cx="10" cy="10" r="9" stroke="currentColor" strokeWidth="1.5"/>
      <path d="M6.5 10L9 12.5L13.5 8" stroke="currentColor" strokeWidth="1.5"
            strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  ),
  error: (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
      <circle cx="10" cy="10" r="9" stroke="currentColor" strokeWidth="1.5"/>
      <path d="M7 7L13 13M13 7L7 13" stroke="currentColor" strokeWidth="1.5"
            strokeLinecap="round"/>
    </svg>
  ),
  info: (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
      <circle cx="10" cy="10" r="9" stroke="currentColor" strokeWidth="1.5"/>
      <path d="M10 9V14M10 6.5V7" stroke="currentColor" strokeWidth="1.5"
            strokeLinecap="round"/>
    </svg>
  ),
};

export default function Toast({ message, type = 'success', onClose }) {
  useEffect(() => {
    const timer = setTimeout(onClose, 3000);
    return () => clearTimeout(timer);
  }, [onClose]);

  const colors = {
    success: { bg: 'var(--accent-lt)', color: 'var(--accent)',   border: '#B6DFC8' },
    error:   { bg: 'var(--danger-lt)', color: 'var(--danger)',   border: '#F5C6C2' },
    info:    { bg: 'var(--warn-lt)',   color: 'var(--warn)',     border: '#FAD9A1' },
  };

  const c = colors[type];

  return (
    <div style={{
      position:     'fixed',
      top:          '24px',
      left:         '50%',
      transform:    'translateX(-50%)',
      zIndex:       9999,
      display:      'flex',
      alignItems:   'center',
      gap:          '10px',
      background:   c.bg,
      color:        c.color,
      border:       `1px solid ${c.border}`,
      borderRadius: '10px',
      padding:      '12px 20px',
      fontSize:     '14px',
      fontWeight:   '500',
      boxShadow:    '0 4px 20px rgba(0,0,0,0.10)',
      animation:    'slideDown 0.25s ease',
      minWidth:     '260px',
      maxWidth:     '420px',
      whiteSpace:   'nowrap',
    }}>
      <style>{`
        @keyframes slideDown {
          from { opacity: 0; transform: translateX(-50%) translateY(-12px); }
          to   { opacity: 1; transform: translateX(-50%) translateY(0); }
        }
      `}</style>
      {icons[type]}
      <span style={{ flex: 1 }}>{message}</span>
      <button onClick={onClose} style={{
        background: 'none',
        color:      c.color,
        opacity:    0.6,
        fontSize:   '18px',
        lineHeight: 1,
        padding:    '0 0 0 8px',
      }}>×</button>
    </div>
  );
}
