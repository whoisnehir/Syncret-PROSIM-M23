import { useState, useEffect, useCallback } from "react";

const API = import.meta.env.VITE_API_URL;

export default function UserManagement({ token, currentUsername }) {
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(false);

  // form creare
  const [newUsername, setNewUsername] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [newRole, setNewRole] = useState("operator");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    try {
      const res = await fetch(`${API}/api/users`, {
        headers: { "Authorization": `Bearer ${token}` },
      });
      if (res.ok) setUsers(await res.json());
    } catch (err) {
      console.error("[UserManagement] Eroare fetch:", err);
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => { fetchUsers(); }, [fetchUsers]);

  const createUser = async () => {
    setError("");
    if (!newUsername.trim() || !newPassword.trim()) {
      setError("Username și parola sunt obligatorii.");
      return;
    }
    setBusy(true);
    try {
      const res = await fetch(`${API}/api/users`, {
        method: "POST",
        headers: { "Content-Type": "application/json", "Authorization": `Bearer ${token}` },
        body: JSON.stringify({ username: newUsername, password: newPassword, role: newRole }),
      });
      if (res.ok) {
        setNewUsername(""); setNewPassword(""); setNewRole("operator");
        await fetchUsers();
      } else {
        const data = await res.json().catch(() => ({}));
        setError(data.error || "Eroare la creare.");
      }
    } catch (err) {
      setError("Eroare de rețea.");
    } finally {
      setBusy(false);
    }
  };

  const deleteUser = async (id, username) => {
    if (!window.confirm(`Sigur ștergi utilizatorul "${username}"?`)) return;
    try {
      await fetch(`${API}/api/users/${id}`, {
        method: "DELETE",
        headers: { "Authorization": `Bearer ${token}` },
      });
      await fetchUsers();
    } catch (err) {
      console.error("[UserManagement] Eroare ștergere:", err);
    }
  };

  return (
    <div style={styles.card}>
      <h2 style={styles.cardTitle}>Gestionare Utilizatori</h2>
      <p style={styles.subtitle}>Creare, listare și ștergere conturi (doar administrator)</p>

      {/* Form creare */}
      <div style={styles.form}>
        <input
          style={styles.input}
          placeholder="Utilizator nou"
          value={newUsername}
          onChange={(e) => setNewUsername(e.target.value)}
        />
        <input
          style={styles.input}
          type="password"
          placeholder="Parolă"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
        />
        <select style={styles.select} value={newRole} onChange={(e) => setNewRole(e.target.value)}>
          <option value="operator">Operator</option>
          <option value="admin">Administrator</option>
        </select>
        <button onClick={createUser} disabled={busy} style={styles.addBtn}>
          {busy ? "..." : "+ Adaugă"}
        </button>
      </div>
      {error && <div style={styles.error}>{error}</div>}

      {/* Tabel utilizatori */}
      <table style={styles.table}>
        <thead>
          <tr>
            <th style={styles.th}>ID</th>
            <th style={styles.th}>Utilizator</th>
            <th style={styles.th}>Rol</th>
            <th style={styles.th}>Acțiuni</th>
          </tr>
        </thead>
        <tbody>
          {users.length === 0 ? (
            <tr><td colSpan={4} style={styles.emptyCell}>
              {loading ? "Se încarcă..." : "Niciun utilizator."}
            </td></tr>
          ) : (
            users.map((u) => (
              <tr key={u.id} style={styles.tr}>
                <td style={styles.td}>{u.id}</td>
                <td style={styles.td}>{u.username}</td>
                <td style={styles.td}>
                  <span style={{
                    ...styles.roleBadge,
                    color: u.role === "admin" ? "#7F77DD" : "#aaa",
                    borderColor: u.role === "admin" ? "#7F77DD" : "#555",
                  }}>
                    {u.role === "admin" ? "Administrator" : "Operator"}
                  </span>
                </td>
                <td style={styles.td}>
                  {u.username === currentUsername ? (
                    <span style={styles.selfLabel}>(cont curent)</span>
                  ) : (
                    <button
                      onClick={() => deleteUser(u.id, u.username)}
                      style={styles.deleteBtn}
                    >
                      Șterge
                    </button>
                  )}
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}

const styles = {
  card: { background: "#252525", borderRadius: 12, padding: 24, border: "1px solid #333" },
  cardTitle: { margin: 0, fontSize: 16, fontWeight: 700, color: "#aaa", textTransform: "uppercase", letterSpacing: 1 },
  subtitle: { color: "#666", fontSize: 12, margin: "8px 0 16px 0" },
  form: { display: "flex", gap: 10, flexWrap: "wrap", marginBottom: 12 },
  input: { flex: "1 1 140px", padding: "10px 12px", background: "#1a1a1a", border: "1px solid #444", borderRadius: 8, color: "#fff", fontSize: 13 },
  select: { padding: "10px 12px", background: "#1a1a1a", border: "1px solid #444", borderRadius: 8, color: "#fff", fontSize: 13 },
  addBtn: { padding: "10px 20px", background: "#7F77DD", border: "none", borderRadius: 8, color: "#fff", fontSize: 13, fontWeight: 700, cursor: "pointer" },
  error: { color: "#E24B4A", fontSize: 13, marginBottom: 12, padding: "8px", background: "rgba(226,74,74,0.1)", borderRadius: 6 },
  table: { width: "100%", borderCollapse: "collapse", fontSize: 13, marginTop: 8 },
  th: { textAlign: "left", padding: "8px 12px", color: "#666", fontWeight: 600, borderBottom: "1px solid #333" },
  tr: { borderBottom: "1px solid #2a2a2a" },
  td: { padding: "10px 12px", color: "#aaa", verticalAlign: "middle" },
  emptyCell: { padding: 24, textAlign: "center", color: "#555" },
  roleBadge: { border: "1px solid", borderRadius: 12, padding: "3px 10px", fontSize: 11, fontWeight: 700 },
  deleteBtn: { background: "rgba(226,74,74,0.1)", border: "1px solid #E24B4A", borderRadius: 6, color: "#E24B4A", padding: "5px 12px", fontSize: 12, cursor: "pointer" },
  selfLabel: { color: "#555", fontSize: 12, fontStyle: "italic" },
};