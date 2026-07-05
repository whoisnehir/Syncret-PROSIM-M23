import { useState } from "react";
import { useAuth } from "../AuthContext";

export default function Login() {
  const { login } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  const handleSubmit = async () => {
    setError("");
    setBusy(true);
    try {
      await login(username, password);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div style={styles.wrapper}>
      <div style={styles.card}>
        <h1 style={styles.logo}>SYNCRET</h1>
        <p style={styles.subtitle}>M23 — Autentificare</p>

        <input
          style={styles.input}
          placeholder="Utilizator"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleSubmit()}
        />
        <input
          style={styles.input}
          type="password"
          placeholder="Parolă"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleSubmit()}
        />

        {error && <div style={styles.error}>{error}</div>}

        <button onClick={handleSubmit} disabled={busy} style={styles.button}>
          {busy ? "Se conectează..." : "Autentificare"}
        </button>

        <div style={styles.hint}>
          -
        </div>
      </div>
    </div>
  );
}

const styles = {
  wrapper: {
    display: "flex", alignItems: "center", justifyContent: "center",
    minHeight: "100vh", background: "#1a1a1a",
  },
  card: {
    background: "#252525", borderRadius: 12, padding: 40,
    border: "1px solid #333", width: 320, textAlign: "center",
  },
  logo: {
    margin: 0, fontSize: 28, fontWeight: 800, color: "#7F77DD", letterSpacing: 2,
  },
  subtitle: { color: "#888", marginBottom: 24, fontSize: 13 },
  input: {
    width: "100%", boxSizing: "border-box", padding: "12px 14px",
    marginBottom: 12, background: "#1a1a1a", border: "1px solid #444",
    borderRadius: 8, color: "#fff", fontSize: 14,
  },
  button: {
    width: "100%", padding: "12px", marginTop: 8, background: "#7F77DD",
    border: "none", borderRadius: 8, color: "#fff", fontSize: 14,
    fontWeight: 700, cursor: "pointer",
  },
  error: {
    color: "#E24B4A", fontSize: 13, marginBottom: 12,
    padding: "8px", background: "rgba(226,74,74,0.1)", borderRadius: 6,
  },
  hint: { color: "#555", fontSize: 11, marginTop: 16 },
};