# 🚀 Module: Event Streaming with Apache Kafka - .NET

HomeGrid — Home Energy Monitor

**Technology Stack:**

- .NET 8 (ASP.NET Core)
- Confluent.Kafka
- Apache Kafka
- Datasette
- SQLite
- Electricity Price API

---

## 🎯 **Scenario**

You've been given a live grid operations dashboard for an electricity company. It's running, but all three panels are empty — no meter readings, no appliance data, no pricing.

Your job: add three sidecars to the workspace, one at a time. Each one wires up a data source. When all three are running, the dashboard tells a complete story.

---

## 📋 Prerequisites

- OpenShift Dev Spaces workspace open with this repository
- Run **"1. Install dependencies"** from the Dev Spaces command palette before starting
- After any workspace restart, re-run **"1. Install dependencies"** before continuing — and once you have created the topic, re-run commands 2, 3 and 4 as well (see Task 4)

---

## 📚 How the app works

| File | Role |
|------|------|
| `src/HomeGrid/Program.cs` | ASP.NET Core minimal API — serves the dashboard, `/api/events`, `/api/appliances`, `/api/price` |
| `src/HomeGrid/MeterReadingConsumer.cs` | `BackgroundService` — polls Kafka for meter readings |
| `src/HomeGrid.Producer/` | Console app — generates fake meter readings |
| `src/HomeGrid.CreateTopic/` | Console app — creates the `meter-readings` topic |
| `src/HomeGrid.Seed/` | Console app — seeds `data/appliances.db` with reference data |
| `sidecar/PriceApi/` | The electricity price API logic, in C# — for reading; the sidecar runs a shared prebuilt image |
| `devfile.yaml` | Workspace definition — **you edit this in Tasks 1, 4, and 6** |

---

## 🧩 **Challenge**

### Task 1 — Add the Kafka sidecar

Open `devfile.yaml` and paste in the following components under the first `TODO` comment — the Kafka broker and [Kafbat UI](https://github.com/kafbat/kafka-ui), a web console for browsing topics, messages and consumer groups:

```yaml
  - name: kafka
    container:
      image: apache/kafka:3.9.0
      endpoints:
        - name: kafka-broker
          exposure: internal
          targetPort: 9092
      env:
        - name: KAFKA_NODE_ID
          value: "1"
        - name: KAFKA_PROCESS_ROLES
          value: broker,controller
        - name: KAFKA_LISTENERS
          value: PLAINTEXT://:9092,CONTROLLER://:9093
        - name: KAFKA_ADVERTISED_LISTENERS
          value: PLAINTEXT://localhost:9092
        - name: KAFKA_CONTROLLER_QUORUM_VOTERS
          value: 1@localhost:9093
        - name: KAFKA_CONTROLLER_LISTENER_NAMES
          value: CONTROLLER
        - name: KAFKA_LISTENER_SECURITY_PROTOCOL_MAP
          value: PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT
        - name: KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR
          value: "1"
        - name: KAFKA_AUTO_CREATE_TOPICS_ENABLE
          value: "false"
      cpuLimit: '1'
      memoryLimit: '1Gi'
  - name: kafka-ui
    container:
      image: ghcr.io/kafbat/kafka-ui:v1.5.0
      endpoints:
        - name: kafka-ui
          exposure: public
          protocol: https
          targetPort: 8081
          path: /
      env:
        - name: SERVER_PORT
          value: "8081"
        - name: KAFKA_CLUSTERS_0_NAME
          value: homegrid
        - name: KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS
          value: localhost:9092
      cpuLimit: '500m'
      memoryLimit: '1Gi'
```

> Kafbat UI listens on port `8081` because the dashboard already uses `8080` — all sidecars in a workspace share one network.

**Save, commit, push, restart the workspace.** Re-run **"1. Install dependencies"**.

---

### Task 2 — Start the dashboard

Run **"2. Start HomeGrid dashboard"** and open the `grid-dashboard` endpoint.

All three panels are empty — there is no topic to read from yet.

---

### Task 3 — Create the topic and watch meter readings flow

Run **"3. Create meter-readings topic"** from the command palette.

```
Topic 'meter-readings' created on localhost:9092
```

Run **"4. Start meter reading producer"** — the Live Readings panel fills with live data.

Open the `kafka-ui` endpoint in a new browser tab. Under **Topics → meter-readings → Messages** you can watch each reading arrive as raw JSON.

---

### Task 4 — Add the datasette sidecar

Open `devfile.yaml` and paste in the following component under the second `TODO` comment:

```yaml
  - name: datasette
    container:
      image: datasetteproject/datasette:0.65.5
      endpoints:
        - name: datasette
          exposure: internal
          targetPort: 8001
      command: ["sh", "-c", "datasette serve \"$PROJECT_SOURCE/data/\" --host 0.0.0.0 --port 8001"]
      cpuLimit: '500m'
      memoryLimit: '512Mi'
      mountSources: true
```

> `mountSources: true` gives datasette access to the project volume, and `$PROJECT_SOURCE` points at this repository inside it. The dashboard reads directly from SQLite — datasette is your browser-based DB explorer.

**Save, commit, push, restart the workspace.** Re-run **"1. Install dependencies"**, then **"2. Start HomeGrid dashboard"**, **"3. Create meter-readings topic"** and **"4. Start meter reading producer"** — a restart stops everything that was running, and the Kafka sidecar does not keep its data, so the topic has to be created again.

---

### Task 5 — Seed and explore the appliance database

Open a terminal and run:

```bash
dotnet run --project src/HomeGrid.Seed
```

This creates `data/appliances.db` with 10 appliances. Open the `datasette` endpoint in a browser tab to explore the data.

Add your own appliances using the SQLite Explorer extension (`zknpr.sqlite-explorer`):
- Open `data/appliances.db` in the editor
- Add rows for any appliances you want to track
- The Appliance Reference panel updates within 2 seconds

---

### Task 6 — Add the electricity price sidecar

Open `sidecar/PriceApi/Program.cs` — take a look at how it works. It returns the current electricity rate based on time of day (off-peak / mid-peak / peak).

> The sidecar you add below runs a prebuilt `electricity-price-api` image that is shared with the Python version of this lab and built from Python source on the cluster. `sidecar/PriceApi/Program.cs` is the same logic written in C#, and `sidecar/Dockerfile` shows how it would be packaged — it is not built or used in this workshop. The dashboard only talks to the price API over HTTP, so it works the same whichever language serves it.

Open `devfile.yaml` and paste the following under the third `TODO` comment:

```yaml
  - name: price-api
    container:
      image: image-registry.openshift-image-registry.svc:5000/workspace-images/electricity-price-api:latest
      endpoints:
        - name: price-api
          exposure: internal
          targetPort: 5000
      cpuLimit: '250m'
      memoryLimit: '256Mi'
```

**Save, commit, push, restart the workspace.** Re-run **"1. Install dependencies"**, then **"2. Start HomeGrid dashboard"**, **"3. Create meter-readings topic"** and **"4. Start meter reading producer"** — a restart stops everything that was running, and the Kafka sidecar does not keep its data, so the topic has to be created again.

The price ticker in the header shows the current rate. Appliance daily costs and circuit reading costs update automatically.

---

### Stretch goal

Stop the dashboard — press `Ctrl+C` in the terminal running **"2. Start HomeGrid dashboard"** — and leave the producer running.

Now check the consumer lag. The Kafka tools live in the `kafka` sidecar, not in your terminal's container, so run them there:

```bash
oc exec $(oc get pod -l controller.devfile.io/devworkspace_id=$DEVWORKSPACE_ID -o name) -c kafka -- \
  /opt/kafka/bin/kafka-consumer-groups.sh \
  --bootstrap-server localhost:9092 \
  --describe \
  --group grid-dashboard
```

Run it a few times and watch the **LAG** column grow — the producer keeps writing a reading every two seconds, but nobody is reading them. Restart the dashboard and run it again: the consumer catches up and the lag drops back to zero.

You can watch the same lag in Kafbat UI under **Consumers → grid-dashboard**.

> In the next session we'll use KEDA to automatically scale the consumer based on this lag.

---

## 🥚 Easter egg

*"Doc Brown needs **1.21 gigawatts** on the **flux-capacitor** circuit. Can you send it through the grid?"*

Hint: you don't need the producer script — Kafbat UI can send a message too. You'll know when you've found it. 🏆
