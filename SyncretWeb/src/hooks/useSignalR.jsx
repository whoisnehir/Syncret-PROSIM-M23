import { useEffect, useState, useRef } from "react";
import * as signalR from "@microsoft/signalr";

const HUB_URL = "https://localhost:7197/hubs/process";

export function useSignalR() {
  const [state, setState] = useState(null);
  const [connected, setConnected] = useState(false);
  const connectionRef = useRef(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL, {
        skipNegotiation: false,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    connection.on("ReceiveState", (data) => {
      setState(data);
    });

    connection.onreconnecting(() => setConnected(false));
    connection.onreconnected(() => setConnected(true));
    connection.onclose(() => setConnected(false));

    connection
      .start()
      .then(() => setConnected(true))
      .catch((err) => console.error("[SignalR] Eroare conectare:", err));

    connectionRef.current = connection;

    return () => {
      connection.stop();
    };
  }, []);

  return { state, connected };
}