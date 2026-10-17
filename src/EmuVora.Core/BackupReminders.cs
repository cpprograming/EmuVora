namespace EmuVora.Core;

public static class BackupReminders
{
    public static bool IsDue(AppSettings settings,DateTimeOffset now)=>settings.BackupReminderDays is 7 or 14 or 30&&(settings.LastBackupAt==null||now-settings.LastBackupAt.Value>=TimeSpan.FromDays(settings.BackupReminderDays));
}
