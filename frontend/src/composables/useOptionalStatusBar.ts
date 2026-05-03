import { ref } from 'vue';
import type { SessionListItemDto, StatusBarDto } from '@/types/api';
import { sessionService } from '@/services/session-service';

function resolveRelevantSession(
  sessions: SessionListItemDto[]
): SessionListItemDto | null {
  if (sessions.length === 0) {
    return null;
  }

  const runningSession = sessions.find((session) => session.status === 'Running');

  if (runningSession) {
    return runningSession;
  }

  const draftSession = sessions.find((session) => session.status === 'Draft');

  if (draftSession) {
    return draftSession;
  }

  const sortedSessions = [...sessions].sort((left, right) => {
    const leftTime = new Date(left.stoppedAtUtc ?? left.createdAtUtc).getTime();
    const rightTime = new Date(right.stoppedAtUtc ?? right.createdAtUtc).getTime();

    return rightTime - leftTime;
  });

  return sortedSessions[0] ?? null;
}

export function useOptionalStatusBar() {
  const statusBar = ref<StatusBarDto | null>(null);

  async function refreshStatusBar(): Promise<void> {
    try {
      const sessions = await sessionService.getAllSessions();
      const relevantSession = resolveRelevantSession(sessions);

      if (!relevantSession) {
        statusBar.value = null;
        return;
      }

      const snapshot = await sessionService.getSessionSnapshot(relevantSession.id);

      statusBar.value = snapshot.statusBar;
    } catch {
      statusBar.value = null;
    }
  }

  return {
    statusBar,
    refreshStatusBar
  };
}
