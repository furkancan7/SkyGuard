# SkyGuard# Sky Guard | Aviation Surveillance & Anomaly Detection

**Sky Guard** ist ein Überwachungssystem, das den Luftverkehr in Echtzeit überwacht, Flüge über die OpenSky Network API integration verfolgt und Abweichungen bzw. Anomalien in den Flugrouten mithilfe von Algorithmen des maschinellen Lernens (Machine Learning) erkennt.

Dieses Repository dient als **technische Portfolio- und Dokumentationsseite**, um die Systemarchitektur, die Designphilosophie und die Leistungsfähigkeit des Projekts zu präsentieren.

---

## 📸 Live-System-Demo
Das Demo-Video zeigt die Benutzeroberfläche, die Dynamik der Datenverarbeitung und die Funktionsweise der Algorithmen zur Erkennung von Anomalien:

[![Sky Guard Demo](youtube.com/watch?si=02NgmE5OxKsAaoSn&v=Zs8tT6mWZjM&feature=youtu.be)
*Hinweis: Das Video demonstriert Schritt für Schritt, wie das System Live-Telemetriedaten verarbeitet, eine Kursabweichung mittels ML-Modell erkennt und den Operator in Echtzeit warnt.*

---

## 🛠 Systemarchitektur & Technologische Basis
Das System basiert auf einer hybriden Architektur, um einen hohen Datenfluss zu optimieren und modular zu arbeiten:

*   **Visuelles Kontrollzentrum (Frontend):** .NET / WPF (Performance-orientierte, minimalistische Bedienoberfläche nach Luftfahrtstandards).
*   **Datenverarbeitung & Analytik (Backend):** Python (Effiziente Datenverarbeitung, Bereinigung und eine präzise strukturierte Engine zur Anomalieerkennung).
*   **Datenquelle:** Echtzeit-Telemetriedaten des Luftraums über die [OpenSky Network API](https://opensky-network.org/).
*   **Analytisches Modell:** Optimierte Algorithmen des maschinellen Lernens zur Erkennung von strukturellen Unregelmäßigkeiten und unerwarteten Flugroutenänderungen.

### 🔄 Datenflussdiagramm
[ OpenSky API ] ➡️ [ Python Datenverarbeitung ] ➡️ [ ML Anomalie-Engine ] ➡️ [ .NET / WPF Kontrollzentrum ]
## 🚀 Hauptmerkmale & Funktionen
*   **Echtzeit-Telemetrieüberwachung:** Live-Verfolgung aktiver Flüge innerhalb definierter Luftraumkoordinaten.
*   **Automatische Kursabweichungsanalyse:** Sofortige Erkennung von Luftfahrzeugen, die sich außerhalb sicherer Flugkorridore bewegen oder unerwartete Höhen-/Geschwindigkeitsänderungen aufweisen.
*   **Situational Awareness Interface:** Ein minimalistisches Dashboard-Design zur Reduzierung der kognitiven Belastung des Operators.
*   **Protokollierung & Berichterstattung:** Systematische Speicherung erkannter Anomalien in einem Datenprotokoll (Log) für retrospektive Analysen.

---

## 🔒 Geistiges Eigentum & Richtlinie zur Code-Vertraulichkeit
Sky Guard ist eine spezialisierte Konzept- und Forschungsarbeit (F&E), die für die Luftsicherheit und Anomalieerkennung entwickelt wurde. Da die spezifische Architektur, die Datenmodelle und die Logik der Algorithmen geschützt sind, werden **der Quellcode sowie ausführbare Dateien (.exe) nicht öffentlich bereitgestellt.**

Für detaillierte technische Präsentationen oder Anfragen zu den Funktionsprinzipien des Systems können Sie mich über die unten stehenden Kanäle kontaktieren.

---

## 💡 Kontakt
Dieses Projekt wurde von **Furkan Can Çelik** entwickelt, um ingenieurtechnische Ansätze im Bereich der Luftfahrttechnologien und Datenanalyse zu demonstrieren.

*   **LinkedIn:** [https://www.linkedin.com/in/furkancancelik7/]
*   **E-Mail:** [furkancancelik480@gmail.com]
*   **GitHub:** [https://github.com/furkancan7]
