<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue';
import AppShell from '@/components/common/AppShell.vue';
import EmptyState from '@/components/common/EmptyState.vue';
import GlobalSnackbar from '@/components/common/GlobalSnackbar.vue';
import LoadingPanel from '@/components/common/LoadingPanel.vue';
import SectionCard from '@/components/common/SectionCard.vue';
import ConfirmDialog from '@/components/common/ConfirmDialog.vue';
import LiveEventBanner from '@/components/dashboard/LiveEventBanner.vue';
import ElevatorShaft from '@/components/dashboard/ElevatorShaft.vue';
import ElevatorTelemetryCard from '@/components/dashboard/ElevatorTelemetryCard.vue';
import RequestPanels from '@/components/dashboard/RequestPanels.vue';
import PassengerTable from '@/components/dashboard/PassengerTable.vue';
import PassengerFormDialog from '@/components/sessions/PassengerFormDialog.vue';
import { usePageAccess } from '@/composables/usePageAccess';
import { useSnackbar } from '@/composables/useSnackbar';
import { authService } from '@/services/auth-service';
import { createSimulationHub, type SimulationHubClient } from '@/services/simulation-hub';
import { sessionService } from '@/services/session-service';
import type {
  CreatePassengerRequestDto,
  SessionListItemDto,
  SessionSnapshotDto,
  SimulationConnectionStatus
} from '@/types/api';
import { extractErrorMessage } from '@/utils/errors';
import { formatFloorCount, formatSessionStatus } from '@/utils/format';
import { canManageSimulation } from '@/utils/roles';
import { buildReportUrl, getQueryParam, updateQueryParam } from '@/utils/urls';

const { currentUser, loading, accessError, initialize } = usePageAccess();
const { snackbar, showError, showInfo, showSuccess, showWarning } = useSnackbar();

const sessions = ref<SessionListItemDto[]>([]);
const snapshot = ref<SessionSnapshotDto | null>(null);

const loadingPage = ref(true);
const refreshingPage = ref(false);
const busyAction = ref(false);
const addPassengerDialogVisible = ref(false);
const locatingPassengerId = ref<string | null>(null);
const stopDialogVisible = ref(false);

const liveStatus = ref<SimulationConnectionStatus>('disconnected');
const lastUpdatedAt = ref<string | null>(null);

const selectedSessionId = ref<string | null>(null);

let hubClient: SimulationHubClient | null = null;
let snapshotUnsubscribe: (() => void) | null = null;
let statusUnsubscribe: (() => void) | null = null;
let pollTimerId: number | null = null;

const canManage = computed(() => canManageSimulation(currentUser.value));

const canIssueGo = computed(() => {
  if (!snapshot.value || !canManage.value) {
    return false;
  }

  return snapshot.value.status === 'Running'
    && snapshot.value.elevator.awaitingGoCommand;
});

const sessionSelectItems = computed(() => {
  return sessions.value.map((session) => ({
    title: `${session.name} • ${formatSessionStatus(session.status)} • ${formatFloorCount(session.floorCount)}`,
    value: session.id
  }));
});

function hasLoadedSession(sessionId: string | null | undefined): sessionId is string {
  return Boolean(sessionId) && sessions.value.some((session) => session.id === sessionId);
}

async function loadSessionList(): Promise<void> {
  sessions.value = await sessionService.getAllSessions();
}

async function fetchSnapshot(sessionId: string): Promise<void> {
  snapshot.value = await sessionService.getSessionSnapshot(sessionId);
  selectedSessionId.value = sessionId;
  lastUpdatedAt.value = new Date().toISOString();
  updateQueryParam('sessionId', sessionId);
}

async function connectToSession(sessionId: string): Promise<void> {
  if (!hubClient) {
    hubClient = createSimulationHub();

    snapshotUnsubscribe = hubClient.onSnapshot((incomingSnapshot) => {
      if (incomingSnapshot.sessionId !== selectedSessionId.value) {
        return;
      }

      snapshot.value = incomingSnapshot;
      lastUpdatedAt.value = new Date().toISOString();
    });

    statusUnsubscribe = hubClient.onStatusChange((status) => {
      liveStatus.value = status;
    });
  }

  await hubClient.joinSession(sessionId);
}

async function selectSession(sessionId: string | null): Promise<void> {
  if (!sessionId) {
    return;
  }

  loadingPage.value = true;

  try {
    await fetchSnapshot(sessionId);
    await connectToSession(sessionId);
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    loadingPage.value = false;
  }
}

async function refreshPageData(): Promise<void> {
  refreshingPage.value = true;

  try {
    await loadSessionList();

    if (sessions.value.length === 0) {
      snapshot.value = null;
      selectedSessionId.value = null;
      lastUpdatedAt.value = null;
      updateQueryParam('sessionId', null);

      if (hubClient) {
        await hubClient.stop();
      }

      return;
    }

    const currentSelectedSessionId = selectedSessionId.value;
    const querySessionId = getQueryParam('sessionId');

    if (hasLoadedSession(currentSelectedSessionId)) {
      await fetchSnapshot(currentSelectedSessionId);
      await connectToSession(currentSelectedSessionId);
      return;
    }

    if (hasLoadedSession(querySessionId)) {
      await fetchSnapshot(querySessionId);
      await connectToSession(querySessionId);
      return;
    }

    try {
      const currentSnapshot = await sessionService.getCurrentSession();

      if (hasLoadedSession(currentSnapshot.sessionId)) {
        snapshot.value = currentSnapshot;
        selectedSessionId.value = currentSnapshot.sessionId;
        updateQueryParam('sessionId', currentSnapshot.sessionId);
        lastUpdatedAt.value = new Date().toISOString();

        await connectToSession(currentSnapshot.sessionId);
        return;
      }
    } catch {
    }

    const fallbackSessionId = sessions.value[0]?.id ?? null;

    if (fallbackSessionId) {
      await fetchSnapshot(fallbackSessionId);
      await connectToSession(fallbackSessionId);
    }
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    refreshingPage.value = false;
  }
}

function startPolling(): void {
  stopPolling();

  pollTimerId = window.setInterval(async () => {
    if (!selectedSessionId.value) {
      return;
    }

    try {
      await fetchSnapshot(selectedSessionId.value);
    } catch {
    }
  }, 5000);
}

function stopPolling(): void {
  if (pollTimerId !== null) {
    window.clearInterval(pollTimerId);
    pollTimerId = null;
  }
}

async function startSelectedSession(): Promise<void> {
  if (!snapshot.value) {
    return;
  }

  busyAction.value = true;

  try {
    snapshot.value = await sessionService.startSession(snapshot.value.sessionId);
    await loadSessionList();
    await connectToSession(snapshot.value.sessionId);

    showSuccess('Сеанс успешно запущен.');
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
  }
}

async function issueGoCommand(): Promise<void> {
  if (!snapshot.value) {
    return;
  }

  busyAction.value = true;

  try {
    snapshot.value = await sessionService.issueGoCommand(snapshot.value.sessionId);
    await loadSessionList();

    showSuccess('Команда «Ход» передана.');
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
  }
}

async function stopSelectedSession(): Promise<void> {
  if (!snapshot.value) {
    return;
  }

  busyAction.value = true;

  try {
    snapshot.value = await sessionService.stopSession(snapshot.value.sessionId);
    await loadSessionList();

    if (snapshot.value.status === 'Stopped') {
      showSuccess('Сеанс остановлен.');
    } else {
      showWarning('Остановка запрошена. Сеанс завершится после выполнения условий остановки.');
    }
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
  }
}

async function addPassenger(request: CreatePassengerRequestDto): Promise<void> {
  if (!snapshot.value) {
    return;
  }

  busyAction.value = true;

  try {
    snapshot.value = await sessionService.createPassenger(snapshot.value.sessionId, request);
    addPassengerDialogVisible.value = false;
    await loadSessionList();

    showSuccess('Пассажир успешно создан.');
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
  }
}

async function locatePassenger(passengerId: string): Promise<void> {
  if (!snapshot.value) {
    return;
  }

  locatingPassengerId.value = passengerId;

  try {
    const location = await sessionService.getPassengerLocation(snapshot.value.sessionId, passengerId);

    showInfo(`Результат опроса: ${location.locationDescription}.`);
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    locatingPassengerId.value = null;
  }
}

async function handleLogout(): Promise<void> {
  await authService.logout();
  window.location.href = '/';
}

onMounted(async () => {
  loadingPage.value = true;

  await initialize();

  if (accessError.value) {
    loadingPage.value = false;
    return;
  }

  try {
    await loadSessionList();

    const sessionIdFromQuery = getQueryParam('sessionId');

    if (hasLoadedSession(sessionIdFromQuery)) {
      await selectSession(sessionIdFromQuery);
      startPolling();
      return;
    }

    if (sessionIdFromQuery) {
      updateQueryParam('sessionId', null);
    }

    try {
      const currentSnapshot = await sessionService.getCurrentSession();

      snapshot.value = currentSnapshot;
      selectedSessionId.value = currentSnapshot.sessionId;
      updateQueryParam('sessionId', currentSnapshot.sessionId);
      lastUpdatedAt.value = new Date().toISOString();

      await connectToSession(currentSnapshot.sessionId);
    } catch {
      if (sessions.value.length > 0) {
        await selectSession(sessions.value[0].id);
      }
    }

    startPolling();
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    loadingPage.value = false;
  }
});

onUnmounted(async () => {
  stopPolling();

  if (snapshotUnsubscribe) {
    snapshotUnsubscribe();
  }

  if (statusUnsubscribe) {
    statusUnsubscribe();
  }

  if (hubClient) {
    await hubClient.stop();
  }
});
</script>

<template>
  <AppShell
    page-title="Мониторинг и управление лифтом"
    active-page="dashboard"
    :current-user="currentUser"
    :status-bar="snapshot?.statusBar"
    @logout="handleLogout"
  >
    <template #toolbar>
      <v-btn
        variant="tonal"
        color="primary"
        prepend-icon="mdi-refresh"
        :loading="refreshingPage"
        @click="refreshPageData"
      >
        Обновить
      </v-btn>
    </template>

    <template v-if="loading || loadingPage">
      <LoadingPanel text="Загрузка данных мониторинга..." />
    </template>

    <template v-else-if="accessError">
      <EmptyState
        title="Недостаточно прав"
        description="У текущего пользователя нет доступа к этой странице."
        icon="mdi-shield-alert-outline"
        action-text="Перейти к входу"
        action-href="/"
      />
    </template>

    <template v-else-if="sessions.length === 0">
      <EmptyState
        title="Сеансы отсутствуют"
        description="Сначала создайте хотя бы один сеанс на странице управления сеансами."
        icon="mdi-calendar-blank-outline"
        action-text="Открыть страницу сеансов"
        action-href="/sessions/"
      />
    </template>

    <template v-else>
      <v-row class="mb-6">
        <v-col cols="12">
          <SectionCard
            title="Выбор и управление активным сеансом"
            subtitle="Ниже можно выбрать сеанс, обновить данные вручную, запустить черновик, подать команду «Ход» или запросить остановку работающего сеанса."
          >
            <template #actions>
              <v-btn
                v-if="snapshot?.status === 'Draft' && canManage"
                color="success"
                variant="flat"
                prepend-icon="mdi-play-circle-outline"
                :loading="busyAction"
                @click="startSelectedSession"
              >
                Старт
              </v-btn>

              <v-btn
                v-if="canIssueGo"
                color="primary"
                variant="flat"
                prepend-icon="mdi-play-outline"
                :loading="busyAction"
                @click="issueGoCommand"
              >
                Ход
              </v-btn>

              <v-btn
                v-if="snapshot?.status === 'Running' && canManage"
                color="warning"
                variant="flat"
                prepend-icon="mdi-stop-circle-outline"
                :loading="busyAction"
                @click="stopDialogVisible = true"
              >
                Стоп
              </v-btn>

              <v-btn
                v-if="snapshot && canManage && snapshot.status !== 'Stopped'"
                color="primary"
                variant="tonal"
                prepend-icon="mdi-account-plus"
                @click="addPassengerDialogVisible = true"
              >
                Добавить пассажира
              </v-btn>

              <v-btn
                v-if="snapshot?.status === 'Stopped'"
                color="secondary"
                variant="tonal"
                prepend-icon="mdi-file-chart"
                :href="buildReportUrl(snapshot.sessionId)"
              >
                К отчёту
              </v-btn>
            </template>

            <v-row>
              <v-col cols="12" lg="7">
                <v-select
                  v-model="selectedSessionId"
                  label="Выбранный сеанс"
                  :items="sessionSelectItems"
                  item-title="title"
                  item-value="value"
                  @update:model-value="selectSession($event)"
                ></v-select>
              </v-col>

              <v-col cols="12" lg="5">
                <LiveEventBanner :status="liveStatus" :updated-at="lastUpdatedAt" />
              </v-col>
            </v-row>

            <v-alert
              v-if="snapshot?.elevator.awaitingGoCommand"
              type="info"
              variant="tonal"
              class="mt-4"
            >
              Обнаружены активные заявки на перемещение. Для начала движения нажмите кнопку «Ход».
            </v-alert>
          </SectionCard>
        </v-col>
      </v-row>

      <template v-if="snapshot">
        <v-row class="mb-6">
          <v-col cols="12" xl="5">
            <SectionCard
              title="Визуализация шахты лифта"
              subtitle="Положение кабины обновляется автоматически через SignalR и периодическую синхронизацию состояния."
            >
              <ElevatorShaft :snapshot="snapshot" />
            </SectionCard>
          </v-col>

          <v-col cols="12" xl="7">
            <ElevatorTelemetryCard :snapshot="snapshot" />
          </v-col>
        </v-row>

        <v-row class="mb-6">
          <v-col cols="12">
            <RequestPanels :snapshot="snapshot" />
          </v-col>
        </v-row>

        <v-row>
          <v-col cols="12">
            <SectionCard
              title="Пассажиры и их положение"
              subtitle="Система предоставляет возможность опроса положения каждого человека в произвольный момент времени."
            >
              <PassengerTable
                :passengers="snapshot.passengers"
                :locating-passenger-id="locatingPassengerId"
                :allow-locate="true"
                @locate="locatePassenger"
              />
            </SectionCard>
          </v-col>
        </v-row>
      </template>
    </template>

    <PassengerFormDialog
      v-model="addPassengerDialogVisible"
      title="Создание нового пассажира"
      :floor-count="snapshot?.floorCount ?? 2"
      :default-source-floor="snapshot?.elevator.currentFloor ?? 1"
      :busy="busyAction"
      @save="addPassenger"
    />

    <ConfirmDialog
      v-model="stopDialogVisible"
      title="Остановка системы"
      text="Подтвердите запрос остановки. Система может быть окончательно остановлена только при пустой кабине лифта."
      confirm-text="Запросить остановку"
      color="warning"
      @confirm="stopSelectedSession"
    />

    <GlobalSnackbar
      v-model="snackbar.visible"
      :text="snackbar.text"
      :color="snackbar.color"
      :timeout="snackbar.timeout"
    />
  </AppShell>
</template>
