import { useState, useEffect, useCallback } from "react";

const API = "https://localhost:7197";

const EVENT_COLORS = {
  MOTOR_START:    "#4CAF50",
  MOTOR_STOP:     "#888",
  START_DENIED:   "#FFA726",
  EMERGENCY_STOP: "#E24B4A",
  ALARM:          "#E24B4A",
  INFO:           "#7F77DD",
};

const COMPONENTS = ["", "B1", "B2", "B3", "B4", "B1_B2", "Clapeta", "System"];
const EVENT_TYPES = ["", "MOTOR_START", "MOTOR_STOP", "START_DENIED", "EMERGENCY_STOP", "ALARM", "INFO"];

export default function EventLog() {
  const [logs, setLogs] = useState([]);
  const [component, setComponent] = useState("");
  const [eventType, setEventType] = useState("");
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);

  const fetchLogs = useCallback(async () => {
    setLoading(true);
    try {
      const params = new URLSearchParams({ page, pageSize: 20 });
      if (component) params.append("component", component);
      if (eventType) params.append("eventType", eventType);

      const res = await fetch(`${API}/api/logs?${params}`);
      const data = await res.json();
      setLogs(data);
    } catch (err) {
      console.error("[EventLog] Eroare fetch:", err);
    } finally {
      setLoading(false);
    }
  }, [component, eventType, page]);

  // refresh automat la 5 secunde
  useEffect(() => {
    fetchLogs();
    const interval = setInterval(fetchLogs, 5000);
    return () => clearInterval(interval);
  }, [fetchLogs]);

  // reset pagina la schimbare filtre
  useEffect(() => { setPage(1); }, [component, eventType]);

  return (
    <div style={styles.card}>
      <h2 style={styles.cardTitle}>Istoric Evenimente</h2>

      {/* Filtre */}
      <div style={styles.filters}>
        <select
          value={component}
          onChange={(e) => setComponent(e.target.value)}
          style={styles.select}
        >
          <option value="">Toate componentele</option>
          {COMPONENTS.filter(Boolean).map((c) => (
            <option key={c} value={c}>{c}</option>
          ))}
        </select>

        <select
          value={eventType}
          onChange={(e) => setEventType(e.target.value)}
          style={styles.select}
        >
          <option value="">Toate tipurile</option>
          {EVENT_TYPES.filter(Boolean).map((t) => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>

        <button onClick={fetchLogs} style={styles.refreshBtn}>
          ↻ Refresh
        </button>
      </div>

      {/* Tabel */}
      <div style={styles.tableWrapper}>
        {loading && <div style={styles.loadingBar} />}
        <table style={styles.table}>
          <thead>
            <tr>
              <th style={styles.th}>Timestamp</th>
              <th style={styles.th}>Component</th>
              <th style={styles.th}>Tip Eveniment</th>
              <th style={styles.th}>Mesaj</th>
            </tr>
          </thead>
          <tbody>
            {logs.length === 0 ? (
              <tr>
                <td colSpan={4} style={styles.emptyCell}>
                  {loading ? "Se încarcă..." : "Niciun eveniment găsit."}
                </td>
              </tr>
            ) : (
              logs.map((log) => (
                <tr key={log.id} style={styles.tr}>
                  <td style={styles.td}>
                    {log.timestamp.replace("T", " ").substring(0, 19).replace(/-/g, ".").replace(/(\d{4})\.(\d{2})\.(\d{2})/, "$3.$2.$1")}
                  </td>
                  <td style={styles.td}>
                    <span style={styles.componentBadge}>{log.component}</span>
                  </td>
                  <td style={styles.td}>
                    <span style={{
                      ...styles.eventBadge,
                      color: EVENT_COLORS[log.eventType] ?? "#aaa",
                      borderColor: EVENT_COLORS[log.eventType] ?? "#444",
                    }}>
                      {log.eventType}
                    </span>
                  </td>
                  <td style={{ ...styles.td, color: "#ccc" }}>{log.message}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Paginare */}
      <div style={styles.pagination}>
        <button
          onClick={() => setPage((p) => Math.max(1, p - 1))}
          disabled={page === 1}
          style={styles.pageBtn}
        >
          ← Anterior
        </button>
        <span style={styles.pageInfo}>Pagina {page}</span>
        <button
          onClick={() => setPage((p) => p + 1)}
          disabled={logs.length < 20}
          style={styles.pageBtn}
        >
          Următor →
        </button>
      </div>
    </div>
  );
}

const styles = {
  card: { background: "#252525", borderRadius: 12, padding: 24, border: "1px solid #333" },
  cardTitle: { margin: "0 0 16px 0", fontSize: 16, fontWeight: 700, color: "#aaa", textTransform: "uppercase", letterSpacing: 1 },
  filters: { display: "flex", gap: 10, marginBottom: 16, flexWrap: "wrap" },
  select: { background: "#1a1a1a", border: "1px solid #444", borderRadius: 6, color: "#ccc", padding: "6px 10px", fontSize: 13, cursor: "pointer" },
  refreshBtn: { background: "#7F77DD22", border: "1px solid #7F77DD", borderRadius: 6, color: "#7F77DD", padding: "6px 14px", fontSize: 13, cursor: "pointer" },
  tableWrapper: { overflowX: "auto", position: "relative" },
  loadingBar: { height: 2, background: "#7F77DD", borderRadius: 1, marginBottom: 4, animation: "slide 1s infinite" },
  table: { width: "100%", borderCollapse: "collapse", fontSize: 13 },
  th: { textAlign: "left", padding: "8px 12px", color: "#666", fontWeight: 600, borderBottom: "1px solid #333", whiteSpace: "nowrap" },
  tr: { borderBottom: "1px solid #2a2a2a", transition: "background 0.15s" },
  td: { padding: "9px 12px", color: "#aaa", verticalAlign: "middle" },
  emptyCell: { padding: 24, textAlign: "center", color: "#555" },
  componentBadge: { background: "#333", borderRadius: 4, padding: "2px 8px", color: "#ccc", fontSize: 12 },
  eventBadge: { border: "1px solid", borderRadius: 4, padding: "2px 8px", fontSize: 12, fontWeight: 600 },
  pagination: { display: "flex", alignItems: "center", justifyContent: "center", gap: 16, marginTop: 16, paddingTop: 16, borderTop: "1px solid #333" },
  pageBtn: { background: "#1a1a1a", border: "1px solid #444", borderRadius: 6, color: "#ccc", padding: "6px 14px", fontSize: 13, cursor: "pointer" },
  pageInfo: { color: "#666", fontSize: 13 },
};