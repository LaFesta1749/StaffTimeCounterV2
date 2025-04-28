# StaffTimeCounterV2

**StaffTimeCounterV2** is a powerful and lightweight **Exiled plugin** for **SCP: Secret Laboratory**, designed specifically to **track and record the playtime** of **Staff Members** (Admins, Moderators, etc.) on your server.

It automatically saves daily playtime statistics into YAML files, provides detailed summaries, and ensures that server staff activity is monitored transparently and reliably.

---

## ✨ Short Description for GitHub

> A high-precision Exiled plugin for SCP:SL that tracks, records, and summarizes staff playtime with daily logging.

---

## ✨ Features

- ✅ Tracks **online time** (minutes played) of each listed staff member.
- ✅ Records data into **daily .yml files** under organized directories.
- ✅ Summarizes multiple days into a **final summary file** via RA or server console command.
- ✅ Supports **automatic folder creation**.
- ✅ **Minimum playtime** detection (skip if <30 seconds).
- ✅ **Debug mode** for easy troubleshooting.
- ✅ Fully configurable via `config.yml`.

---

## 📁 Folder Structure

```
EXILED/Configs/StaffTimeCounterV2/
|
├── config.yml            # Staff members listed here
|
├── Data/
    |
    ├── Times/             # Daily playtime logs per staff member
    |
    └── Summaries/         # Summarized reports over time periods
```

---

## ⚙️ Configuration for Exiled Config file

```yaml
stcv2:
  is_enabled: true
  debug: false
```

## ⚙️ Configuration located in EXILED/Configs/StaffTimeCounterV2/Config/config.yml

```yaml
staff_members:
  XXXXXXXXXXXX@steam: owner #name
  XXXXXXXXXXXX@steam: head_of_staff #name
  XXXXXXXXXXXX@steam: moderator #name
  # Add more SteamIDs here...
```

---

## 🔧 Commands

| Command              | Description                                      | Where to use                   |
|:---------------------|:-------------------------------------------------|:-------------------------------|
| `stc_summary Admin`   | Generates a summary from existing Times records. | RA Console or Server Console   |

> **Note:** Command is **case-insensitive**.

---

## 🚫 Notes

- Only **staff members** listed in `config.yml` are tracked.
- Players disconnected **under 30 seconds** are ignored.
- Files are written with **local server time** (not UTC).
- Daily files are named like: `StaffTimeCounter_Day_29_04_2025.yml` (`date_month_year`)

---

## ❤  Credits

- Plugin created by **LaFesta1749**.
- Built with love and precision using the **Exiled 9.6.0-beta7** framework.

---

# 🎉 Thank you for using StaffTimeCounterV2!
> Transparency and accountability made easy for your SCP:SL server.

