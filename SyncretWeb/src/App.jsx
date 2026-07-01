import { useSignalR } from "./hooks/useSignalR";
import { useAuth } from "./AuthContext";
import BandStatus from "./components/BandStatus";
import EventLog from "./components/EventLog";
import EventChart from "./components/EventChart";
import Login from "./components/Login";
import ControlLog from "./components/ControlLog";
import UserManagement from "./components/UserManagement";

export default function App() {
  const { auth, logout } = useAuth();
  const { state, connected } = useSignalR();

  // Dacă nu e autentificat → pagina de login
  if (!auth) {
    return <Login />;
  }

  const isAdmin = auth.role === "admin";

  return (
    <div style={styles.root}>
      {/* Header */}
      <header style={styles.header}>
        <div style={styles.headerLeft}>
          <div style={styles.logo}>SYNCRET</div>
          <div style={styles.subtitle}>M23 — Sistem de Încărcare cu Benzi</div>
        </div>
        <div style={styles.headerRight}>
          <div style={styles.userBadge}>
            <span style={styles.userName}>{auth.username}</span>
            <span style={{
              ...styles.roleBadge,
              background: isAdmin ? "rgba(127,119,221,0.2)" : "rgba(136,136,136,0.2)",
              color: isAdmin ? "#7F77DD" : "#aaa",
              borderColor: isAdmin ? "#7F77DD" : "#555",
            }}>
              {isAdmin ? "Administrator" : "Operator"}
            </span>
          </div>
          <div style={styles.connectionBadge}>
            <div style={{
              ...styles.dot,
              background: connected ? "#4CAF50" : "#E24B4A",
              boxShadow: connected ? "0 0 8px #4CAF50" : "0 0 8px #E24B4A",
            }} />
            <span style={{ color: connected ? "#4CAF50" : "#E24B4A", fontSize: 13, fontWeight: 600 }}>
              {connected ? "LIVE" : "DECONECTAT"}
            </span>
          </div>
          <button onClick={logout} style={styles.logoutBtn}>Ieșire</button>
        </div>
      </header>

      {/* grid principal */}
      <main style={styles.main}>
        <div style={styles.fullWidth}>
          <BandStatus state={state} token={auth.token} isAdmin={isAdmin} />
        </div>
        <div style={styles.twoCol}>
          <EventChart />
          <EventLog />
        </div>
        {isAdmin && (
          <div style={styles.fullWidth}>
            <ControlLog token={auth.token} />
          </div>
        )}
        {isAdmin && (
          <div style={styles.fullWidth}>
            <UserManagement token={auth.token} currentUsername={auth.username} />
          </div>
        )}
      </main>

      {/* footer */}
      <footer style={styles.footer}>
        Syncret M23 Monitor · Tier 3 Web Dashboard
      </footer>
    </div>
  );
}

const styles = {
  root: {
    minHeight: "100vh",
    background: "#1E1E1E",
    color: "#fff",
    fontFamily: "'Segoe UI', system-ui, sans-serif",
    display: "flex",
    flexDirection: "column",
  },
  header: {
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    padding: "16px 32px",
    borderBottom: "1px solid #2a2a2a",
    background: "#1a1a1a",
  },
  headerLeft: { display: "flex", alignItems: "center", gap: 20 },
  headerRight: { display: "flex", alignItems: "center", gap: 16 },
  logo: { fontSize: 22, fontWeight: 800, color: "#7F77DD", letterSpacing: 2 },
  subtitle: { fontSize: 13, color: "#666", fontWeight: 400 },
  userBadge: { display: "flex", alignItems: "center", gap: 8 },
  userName: { fontSize: 13, color: "#ccc", fontWeight: 600 },
  roleBadge: {
    fontSize: 11, fontWeight: 700, padding: "3px 10px",
    borderRadius: 12, border: "1px solid",
  },
  connectionBadge: {
    display: "flex",
    alignItems: "center",
    gap: 8,
    padding: "6px 16px",
    borderRadius: 20,
    background: "#252525",
    border: "1px solid #333",
  },
  dot: { width: 8, height: 8, borderRadius: "50%", transition: "all 0.3s" },
  logoutBtn: {
    background: "transparent", border: "1px solid #444", borderRadius: 8,
    color: "#aaa", padding: "6px 14px", fontSize: 13, cursor: "pointer",
  },
  main: {
    flex: 1,
    padding: "24px 32px",
    display: "flex",
    flexDirection: "column",
    gap: 20,
    maxWidth: 1400,
    width: "100%",
    margin: "0 auto",
    boxSizing: "border-box",
  },
  fullWidth: { width: "100%" },
  twoCol: {
    display: "grid",
    gridTemplateColumns: "1fr 1.6fr",
    gap: 20,
    alignItems: "start",
  },
  footer: {
    textAlign: "center",
    padding: "12px",
    fontSize: 12,
    color: "#444",
    borderTop: "1px solid #2a2a2a",
  },
};