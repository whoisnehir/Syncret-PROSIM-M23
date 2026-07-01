import { useState } from "react";

const API = import.meta.env.VITE_API_URL;

const BANDS = [
  { key: "m1", label: "B1", sublabel: "Intrare Stânga" },
  { key: "m2", label: "B2", sublabel: "Intrare Dreapta" },
  { key: "m3", label: "B3", sublabel: "Ieșire Stânga" },
  { key: "m4", label: "B4", sublabel: "Ieșire Dreapta" },
];

const CLAPETA_LABELS = {
  S6: "Stânga (S6)",
  S7: "Mijloc (S7)",
  S8: "Dreapta (S8)",
  None: "—",
};

function formatTime(raw) {
  if (!raw) return "--:--:--";
  const utc = raw.endsWith("Z") ? raw : raw + "Z";
  return new Date(utc).toLocaleTimeString("ro-RO", {
    hour: "2-digit", minute: "2-digit", second: "2-digit",
  });
}

export default function BandStatus({ state, token, isAdmin }) {
  const [busy, setBusy] = useState(false);

  const toggleRunning = async () => {
    if (!state) return;
    setBusy(true);
    try {
      await fetch(`${API}/api/control`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${token}`,
        },
        body: JSON.stringify({ isRunning: !state.isRunning }),
      });
    } catch (err) {
      console.error("[BandStatus] Eroare control:", err);
    } finally {
      setBusy(false);
    }
  };

  if (!state) {
    return (
      <div style={styles.card}>
        <h2 style={styles.cardTitle}>Stare Proces</h2>
        <p style={styles.waiting}>Așteptare date...</p>
      </div>
    );
  }

  return (
    <div style={styles.card}>
      <div style={styles.headerRow}>
        <h2 style={styles.cardTitle}>Stare Proces</h2>
        {isAdmin && (
          <button
            onClick={toggleRunning}
            disabled={busy}
            style={{
              ...styles.controlBtn,
              background: state.isRunning ? "rgba(226,74,74,0.15)" : "rgba(76,175,80,0.15)",
              borderColor: state.isRunning ? "#E24B4A" : "#4CAF50",
              color: state.isRunning ? "#E24B4A" : "#4CAF50",
              opacity: busy ? 0.5 : 1,
              cursor: busy ? "wait" : "pointer",
            }}
          >
            {state.isRunning ? "⏹ OPREȘTE" : "▶ PORNEȘTE"}
          </button>
        )}
      </div>

      {/* Banner proces oprit */}
      {!state.isRunning && (
        <div style={styles.stoppedBanner}>
          ⏸ PROCES OPRIT — Comandă din interfața web
        </div>
      )}

      {/* Alarmă */}
      {state.isAlarm && (
        <div style={styles.alarmBanner}>
          ⚠ ALARMĂ ACTIVĂ — Conflict Clapetă
        </div>
      )}

      {/* benzile */}
      <div style={styles.bandsGrid}>
        {BANDS.map(({ key, label, sublabel }) => {
          const active = state[key];
          return (
            <div
              key={key}
              style={{
                ...styles.bandCard,
                borderColor: active ? "#4CAF50" : "#444",
                background: active ? "rgba(76,175,80,0.10)" : "rgba(255,255,255,0.03)",
              }}
            >
              <div style={{ ...styles.bandIndicator, background: active ? "#4CAF50" : "#555" }} />
              <div>
                <div style={styles.bandLabel}>{label}</div>
                <div style={styles.bandSublabel}>{sublabel}</div>
              </div>
              <div style={{ ...styles.bandStatus, color: active ? "#4CAF50" : "#888" }}>
                {active ? "ACTIV" : "OPRIT"}
              </div>
            </div>
          );
        })}
      </div>

      {/* clapeta si timestamp */}
      <div style={styles.footer}>
        <span>Clapetă: <strong style={{ color: "#7F77DD" }}>{CLAPETA_LABELS[state.clapetaPos] ?? state.clapetaPos}</strong></span>
        <span style={styles.timestamp}>
          Actualizat: {formatTime(state.updatedAt)}
        </span>
      </div>
    </div>
  );
}

const styles = {
  card: {
    background: "#252525",
    borderRadius: 12,
    padding: "24px",
    border: "1px solid #333",
  },
  headerRow: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 20,
  },
  cardTitle: {
    margin: 0,
    fontSize: 16,
    fontWeight: 700,
    color: "#aaa",
    textTransform: "uppercase",
    letterSpacing: 1,
  },
  controlBtn: {
    border: "1px solid",
    borderRadius: 8,
    padding: "8px 16px",
    fontSize: 13,
    fontWeight: 700,
    transition: "all 0.2s",
  },
  waiting: { color: "#666", fontStyle: "italic" },
  stoppedBanner: {
    background: "rgba(136,136,136,0.15)",
    border: "1px solid #888",
    borderRadius: 8,
    padding: "10px 16px",
    color: "#aaa",
    fontWeight: 700,
    marginBottom: 16,
  },
  alarmBanner: {
    background: "rgba(226,74,74,0.15)",
    border: "1px solid #E24B4A",
    borderRadius: 8,
    padding: "10px 16px",
    color: "#E24B4A",
    fontWeight: 700,
    marginBottom: 16,
    animation: "pulse 1s infinite",
  },
  bandsGrid: {
    display: "grid",
    gridTemplateColumns: "1fr 1fr",
    gap: 12,
    marginBottom: 16,
  },
  bandCard: {
    display: "flex",
    alignItems: "center",
    gap: 12,
    padding: "14px 16px",
    borderRadius: 10,
    border: "1px solid",
    transition: "all 0.3s ease",
  },
  bandIndicator: {
    width: 14,
    height: 14,
    borderRadius: "50%",
    flexShrink: 0,
    transition: "background 0.3s ease",
  },
  bandLabel: { fontSize: 16, fontWeight: 700, color: "#fff" },
  bandSublabel: { fontSize: 12, color: "#888", marginTop: 2 },
  bandStatus: { marginLeft: "auto", fontSize: 12, fontWeight: 700 },
  footer: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    paddingTop: 16,
    borderTop: "1px solid #333",
    fontSize: 13,
    color: "#888",
  },
  timestamp: { fontSize: 12, color: "#555" },
};