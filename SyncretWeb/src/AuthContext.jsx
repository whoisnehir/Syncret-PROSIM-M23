import { createContext, useContext, useState } from "react";

const API = import.meta.env.VITE_API_URL;
const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [auth, setAuth] = useState(null); // { token, username, role }

  const login = async (username, password) => {
    const res = await fetch(`${API}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password }),
    });
    if (!res.ok) throw new Error("Utilizator sau parolă incorectă");
    const data = await res.json();
    setAuth({ token: data.token, username: data.username, role: data.role });
  };

  const logout = () => setAuth(null);

  return (
    <AuthContext.Provider value={{ auth, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  return useContext(AuthContext);
}