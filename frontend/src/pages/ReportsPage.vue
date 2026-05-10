<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue';
import AppShell from '@/components/common/AppShell.vue';
import EmptyState from '@/components/common/EmptyState.vue';
import GlobalSnackbar from '@/components/common/GlobalSnackbar.vue';
import LoadingPanel from '@/components/common/LoadingPanel.vue';
import SectionCard from '@/components/common/SectionCard.vue';
import PassengerTable from '@/components/dashboard/PassengerTable.vue';
import ReportSummaryCard from '@/components/reports/ReportSummaryCard.vue';
import ReportsTable from '@/components/reports/ReportsTable.vue';
import { useOptionalStatusBar } from '@/composables/useOptionalStatusBar';
import { usePageAccess } from '@/composables/usePageAccess';
import { useSnackbar } from '@/composables/useSnackbar';
import { authService } from '@/services/auth-service';
import { reportService } from '@/services/report-service';
import { sessionService } from '@/services/session-service';
import type { SessionListItemDto, SessionReportDto } from '@/types/api';
import { extractErrorMessage } from '@/utils/errors';
import { getQueryParam, updateQueryParam } from '@/utils/urls';

const { currentUser, loading, accessError, initialize } = usePageAccess();
const { statusBar, refreshStatusBar } = useOptionalStatusBar();
const { snackbar, showError, showSuccess } = useSnackbar();

const sessions = ref<SessionListItemDto[]>([]);
const selectedReport = ref<SessionReportDto | null>(null);
const reportSectionRef = ref<HTMLElement | null>(null);

const loadingReports = ref(true);
const loadingReportDetails = ref(false);
const exporting = ref(false);

const selectedSessionId = computed(() => selectedReport.value?.sessionId ?? null);

async function loadSessions(): Promise<void> {
  const allSessions = await sessionService.getAllSessions();

  sessions.value = allSessions.filter((session) => session.status === 'Stopped' && session.hasReport);
}

async function loadReport(
  sessionId: string,
  scrollToReportSection = false
): Promise<void> {
  loadingReportDetails.value = true;

  try {
    selectedReport.value = await reportService.getReport(sessionId);
    updateQueryParam('sessionId', sessionId);

    if (scrollToReportSection) {
      await nextTick();
      reportSectionRef.value?.scrollIntoView({
        behavior: 'smooth',
        block: 'start'
      });
    }
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    loadingReportDetails.value = false;
  }
}

async function handleSelectReport(sessionId: string): Promise<void> {
  await loadReport(sessionId, true);
  await refreshStatusBar();
}

async function refreshData(): Promise<void> {
  const querySessionId = getQueryParam('sessionId');
  const preferredSessionId = selectedReport.value?.sessionId ?? querySessionId;

  await loadSessions();

  if (sessions.value.length === 0) {
    selectedReport.value = null;
    updateQueryParam('sessionId', null);
    await refreshStatusBar();
    return;
  }

  if (preferredSessionId && sessions.value.some((session) => session.id === preferredSessionId)) {
    await loadReport(preferredSessionId, false);
    await refreshStatusBar();
    return;
  }

  await loadReport(sessions.value[0].id, false);
  await refreshStatusBar();
}

async function exportExcel(): Promise<void> {
  if (!selectedReport.value) {
    return;
  }

  exporting.value = true;

  try {
    const fileResult = await reportService.downloadExcel(selectedReport.value.sessionId);
    const objectUrl = URL.createObjectURL(fileResult.blob);

    const link = document.createElement('a');
    link.href = objectUrl;
    link.download = fileResult.fileName;
    link.click();

    URL.revokeObjectURL(objectUrl);

    showSuccess('Файл Excel успешно сформирован и отправлен на скачивание.');
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    exporting.value = false;
  }
}

async function handleLogout(): Promise<void> {
  await authService.logout();
  window.location.href = '/';
}

onMounted(async () => {
  loadingReports.value = true;

  await initialize();

  if (accessError.value) {
    loadingReports.value = false;
    return;
  }

  try {
    await refreshData();
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    loadingReports.value = false;
  }
});
</script>

<template>
  <AppShell
    page-title="Итоговые отчёты"
    active-page="reports"
    :current-user="currentUser"
    :status-bar="statusBar"
    @logout="handleLogout"
  >
    <template #toolbar>
      <v-btn
        variant="tonal"
        color="primary"
        prepend-icon="mdi-refresh"
        :loading="loadingReports || loadingReportDetails"
        @click="refreshData"
      >
        Обновить список
      </v-btn>
    </template>

    <template v-if="loading || loadingReports">
      <LoadingPanel text="Загрузка списка доступных отчётов..." />
    </template>

    <template v-else-if="accessError">
      <EmptyState
        title="Недостаточно прав"
        description="У текущего пользователя нет доступа к этой странице."
        icon="mdi-shield-alert-outline"
        action-text="К мониторингу"
        action-href="/dashboard/"
      />
    </template>

    <template v-else-if="sessions.length === 0">
      <EmptyState
        title="Отчёты отсутствуют"
        description="Сначала завершите хотя бы один сеанс с корректной остановкой системы."
        icon="mdi-file-document-outline"
        action-text="Открыть мониторинг"
        action-href="/dashboard/"
      />
    </template>

    <template v-else>
      <v-row class="mb-6">
        <v-col cols="12">
          <SectionCard
            title="Список завершённых сеансов"
            subtitle="Выберите завершённый сеанс, для которого требуется открыть итоговый отчёт."
          >
            <ReportsTable
              :sessions="sessions"
              :selected-session-id="selectedSessionId"
              @select="handleSelectReport"
            />
          </SectionCard>
        </v-col>
      </v-row>

      <div ref="reportSectionRef">
        <v-row class="mb-6">
          <v-col cols="12">
            <ReportSummaryCard :report="selectedReport" />
          </v-col>
        </v-row>

        <v-row v-if="selectedReport" class="mb-6">
          <v-col cols="12">
            <SectionCard
              title="Пассажиры завершённого сеанса"
              subtitle="Таблица фиксирует финальное состояние каждого пассажира на момент формирования итогового отчёта."
            >
              <template #actions>
                <v-btn
                  color="success"
                  variant="flat"
                  prepend-icon="mdi-file-excel"
                  :loading="exporting"
                  @click="exportExcel"
                >
                  Экспорт в Excel
                </v-btn>
              </template>

              <PassengerTable
                :passengers="selectedReport.passengers"
                :allow-locate="false"
              />
            </SectionCard>
          </v-col>
        </v-row>
      </div>
    </template>

    <GlobalSnackbar
      v-model="snackbar.visible"
      :text="snackbar.text"
      :color="snackbar.color"
      :timeout="snackbar.timeout"
    />
  </AppShell>
</template>
