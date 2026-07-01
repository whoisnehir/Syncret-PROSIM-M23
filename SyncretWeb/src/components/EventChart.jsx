import { useState, useEffect } from "react";
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
  Tooltip, Legend, ResponsiveContainer
} from "recharts";

const API = import.meta.env.VITE_API_URL;

const EVENT_COLORS_CHART = {
  MOTOR_START:    "#4CAF50",
  MOTOR_STOP:     "#888888",
  START_DENIED:   "#FFA726",
  EMERGENCY_STOP: "#E24B4A",
  ALARM:          "#E24B4A",
  INFO:           "#7F77DD",
};

export default function EventChart() {
  const [chartData, setChartData] = useState([]);
  const [eventTypes, setEventTypes] = useState([]);
  const [lastHours, setLastHours] = useState(24);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const fetchStats = async () => {
      setLoading(true);
      try {
        const res = await fetch(`${API}/api/stats?lastHours=${lastHours}`);
        const raw = await res.json();

        // Transformare date: [{hour, eventType, count}] → [{hour, MOTOR_START: n, ALARM: n, ...}]
        const grouped = {};
        const types = new Set();

        raw.forEach(({ hour, eventType, count }) => {
          const label = new Date(hour).toLocaleTimeString("ro-RO", {
            hour: "2-digit", minute: "2-digit"
          });
          if (!grouped[label]) grouped[label] = { hour: label };
          grouped[label][eventType] = count;
          types.add(eventType);
        });

        setChartData(Object.values(grouped));
        setEventTypes([...types]);
      } catch (err) {
        console.error("[EventChart] Eroare fetch:", err);
      } finally {
        setLoading(false);
      }
    };

    fetchStats();
    const interval = setInterval(fetchStats, 30000); 
    return () => clearInterval(interval);
  }, [lastHours]);

  return (
    <div style={styles.card}>
      <div style={styles.header}>
        <h2 style={styles.cardTitle}>Evenimente pe Oră</h2>
        <div style={styles.controls}>
          {[6, 12, 24].map((h) => (
            <button
              key={h}
              onClick={() => setLastHours(h)}
              style={{
                ...styles.filterBtn,
                background: lastHours === h ? "#7F77DD33" : "transparent",
                borderColor: lastHours === h ? "#7F77DD" : "#444",
                color: lastHours === h ? "#7F77DD" : "#888",
              }}
            >
              {h}h
            </button>
          ))}
        </div>
      </div>

      {loading && <div style={styles.loadingBar} />}

      {chartData.length === 0 ? (
        <div style={styles.empty}>
          {loading ? "Se încarcă..." : "Nu există date pentru intervalul selectat."}
        </div>
      ) : (
        <ResponsiveContainer width="100%" height={280}>
          <BarChart data={chartData} margin={{ top: 8, right: 16, left: 0, bottom: 8 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#333" />
            <XAxis
              dataKey="hour"
              tick={{ fill: "#666", fontSize: 11 }}
              axisLine={{ stroke: "#444" }}
              tickLine={false}
            />
            <YAxis
              tick={{ fill: "#666", fontSize: 11 }}
              axisLine={false}
              tickLine={false}
              allowDecimals={false}
            />
            <Tooltip
              contentStyle={{
                background: "#1a1a1a",
                border: "1px solid #444",
                borderRadius: 8,
                fontSize: 12,
              }}
              labelStyle={{ color: "#aaa", marginBottom: 4 }}
            />
            <Legend
              wrapperStyle={{ fontSize: 12, color: "#888", paddingTop: 8 }}
            />
            {eventTypes.map((type) => (
              <Bar
                key={type}
                dataKey={type}
                stackId="a"
                fill={EVENT_COLORS_CHART[type] ?? "#7F77DD"}
                radius={type === eventTypes[eventTypes.length - 1] ? [4, 4, 0, 0] : [0, 0, 0, 0]}
              />
            ))}
          </BarChart>
        </ResponsiveContainer>
      )}
    </div>
  );
}

const styles = {
  card: { background: "#252525", borderRadius: 12, padding: 24, border: "1px solid #333" },
  header: { display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 },
  cardTitle: { margin: 0, fontSize: 16, fontWeight: 700, color: "#aaa", textTransform: "uppercase", letterSpacing: 1 },
  controls: { display: "flex", gap: 8 },
  filterBtn: { border: "1px solid", borderRadius: 6, padding: "4px 12px", fontSize: 12, cursor: "pointer", transition: "all 0.2s" },
  loadingBar: { height: 2, background: "#7F77DD", borderRadius: 1, marginBottom: 8 },
  empty: { height: 280, display: "flex", alignItems: "center", justifyContent: "center", color: "#555", fontStyle: "italic" },
};