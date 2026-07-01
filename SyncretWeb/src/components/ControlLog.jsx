import { useState, useEffect, useCallback } from "react";

const API = import.meta.env.VITE_API_URL;

function formatTimestamp(raw) {
  if (!raw) return "";
  const utc = raw.endsWith("Z") ? raw : raw + "Z";
  return new Date(utc).toLocaleString("ro-RO", {
    day: "2-digit", month: "2-digit", year: "numeric",
    hour: "2-digit", minute: "2-digit", second: "2-digit",
  });
}

export default function ControlLog({ token }) {
  const [entries, setEntries] = useState([]);
  const [loading, setLoading] = useState(false);

  const fetchLog = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetch(`${API}/api/control-log`, {
        headers: { "Authorization": `Bearer ${token}` },
      });
      if (res.ok) {
        setEntries(await res.json());
      }
    } catch (err) {
      console.error("[ControlLog] Eroare fetch:", err);
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    fetchLog();
    const interval = setInterval(fetchLog, 5000);
    return () => clearInterval(interval);
  }, [fetchLog]);

  return (
    <div style={styles.card}>
      <div style={styles.header}>
        <h2 style={styles.cardTitle}>Raport Întreruperi Proces</h2>
        <button onClick={fetchLog} style={styles.refreshBtn}>↻ Refresh</button>
      </div>
      <p style={styles.subtitle}>
        Istoric comenzi de oprire și repornire (vizibil doar administratorului)
      </p>

      <div style={styles.tableWrapper}>
        {loading && <div style={styles.loadingBar} />}
        <table style={styles.table}>
          <thead>
            <tr>
              <th style={styles.th}>Data / Ora</th>
              <th style={styles.th}>Utilizator</th>
              <th style={styles.th}>Acțiune</th>
            </tr>
          </thead>
          <tbody>
            {entries.length === 0 ? (
              <tr>
                <td colSpan={3} style={styles.emptyCell}>
                  {loading ? "Se încarcă..." : "Nicio comandă înregistrată."}
                </td>
              </tr>
            ) : (
              entries.map((e) => (
                <tr key={e.id} style={styles.tr}>
                  <td style={styles.td}>{formatTimestamp(e.timestamp)}</td>
                  <td style={styles.td}>
                    <span style={styles.userBadge}>{e.username}</span>
                  </td>
                  <td style={styles.td}>
                    <span style={{
                      ...styles.actionBadge,
                      color: e.action === "STOP" ? "#E24B4A" : "#4CAF50",
                      borderColor: e.action === "STOP" ? "#E24B4A" : "#4CAF50",
                    }}>
                      {e.action === "STOP" ? "⏹ OPRIRE" : "▶ REPORNIRE"}
                    </span>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

const styles = {
  card: { background: "#252525", borderRadius: 12, padding: 24, border: "1px solid #333" },
  header: { display: "flex", justifyContent: "space-between", alignItems: "center" },
  cardTitle: { margin: 0, fontSize: 16, fontWeight: 700, color: "#aaa", textTransform: "uppercase", letterSpacing: 1 },
  subtitle: { color: "#666", fontSize: 12, margin: "8px 0 16px 0" },
  refreshBtn: { background: "#7F77DD22", border: "1px solid #7F77DD", borderRadius: 6, color: "#7F77DD", padding: "6px 14px", fontSize: 13, cursor: "pointer" },
  tableWrapper: { overflowX: "auto", position: "relative" },
  loadingBar: { height: 2, background: "#7F77DD", borderRadius: 1, marginBottom: 4 },
  table: { width: "100%", borderCollapse: "collapse", fontSize: 13 },
  th: { textAlign: "left", padding: "8px 12px", color: "#666", fontWeight: 600, borderBottom: "1px solid #333", whiteSpace: "nowrap" },
  tr: { borderBottom: "1px solid #2a2a2a" },
  td: { padding: "9px 12px", color: "#aaa", verticalAlign: "middle" },
  emptyCell: { padding: 24, textAlign: "center", color: "#555" },
  userBadge: { background: "#333", borderRadius: 4, padding: "2px 8px", color: "#ccc", fontSize: 12 },
  actionBadge: { border: "1px solid", borderRadius: 4, padding: "2px 8px", fontSize: 12, fontWeight: 600 },
};