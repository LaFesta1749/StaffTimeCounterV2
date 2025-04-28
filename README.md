# StaffTimeCounterV2

Track your staff members' playtime automatically, generate detailed daily logs, and compile weekly reports with ease.

---

## Overview
**StaffTimeCounterV2** is a plugin for SCP: Secret Laboratory servers (LabAPI) that monitors specific staff members' active time on the server. It creates daily records and can generate comprehensive summary reports. Perfect for servers that require staff time accountability.

---

## Features
- 🕒 Real-time tracking of staff members' online time.
- 📅 Daily YAML files recording individual staff activity.
- 🔹 Weekly or manual summary generation.
- 🔸 Automatic creation of `config.yml` if missing.
- ⚡ Stable and lightweight (optimized for LabAPI servers).
- ✅ Easy integration into your current server setup.

---

## Installation

1. Download the latest compiled `StaffTimeCounterV2.dll` from the releases.
2. Place the DLL into your server's `plugins/StaffTimeCounterV2/` directory.
3. Start your server once to auto-generate the `config.yml` file.
4. Edit `config.yml` to add your staff members. Example:

```yaml
76561198047345881@steam: owner
76561199048565475@steam: head_of_staff
```

5. Restart the server. Done!

---

## Configuration Files

- **config.yml**: Contains the list of tracked staff members and their ranks.
- **Times Folder**: Daily YAML files for each tracked day.
- **Summaries Folder**: Generated summary files containing combined times over periods.

---

## Commands

| Command | Description |
|:---|:---|
| `.stc_summary admin` | Manually generate an admin-specific summary report. |
| `.stc_summary user` | Manually generate a user-specific summary report. |

You can use these commands via **RemoteAdmin** or **GameConsole**.

---

## Requirements
- SCP: Secret Laboratory Server running LabAPI.
- LabAPI Loader (properly installed).

---

## Notes
- The plugin automatically skips today's active file when generating summaries.
- Daily records will be deleted after being included in a summary.
- The plugin is designed to fail gracefully if no staff are configured.

---

## Future Plans
- Optional Discord Webhook notifications for summaries.
- Playtime goals and auto-promotions based on total playtime.
- Better admin control over records (reset, manual correction).

---

## Credits
Created and maintained by **LaFesta1749**.

---

## License
This project is licensed under the MIT License.

