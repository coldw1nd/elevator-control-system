<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import AppShell from '@/components/common/AppShell.vue';
import ConfirmDialog from '@/components/common/ConfirmDialog.vue';
import EmptyState from '@/components/common/EmptyState.vue';
import GlobalSnackbar from '@/components/common/GlobalSnackbar.vue';
import LoadingPanel from '@/components/common/LoadingPanel.vue';
import SectionCard from '@/components/common/SectionCard.vue';
import StatTile from '@/components/common/StatTile.vue';
import DraftPassengerTable from '@/components/sessions/DraftPassengerTable.vue';
import PassengerFormDialog from '@/components/sessions/PassengerFormDialog.vue';
import SessionCreatorCard from '@/components/sessions/SessionCreatorCard.vue';
import SessionsTable from '@/components/sessions/SessionsTable.vue';
import { usePageAccess } from '@/composables/usePageAccess';
import { useSnackbar } from '@/composables/useSnackbar';
import { authService } from '@/services/auth-service';
import { sessionService } from '@/services/session-service';
import type {
  CreatePassengerRequestDto,
  CreateSessionRequestDto,
  PassengerDto,
  SessionListItemDto,
  SessionSnapshotDto,
  UpdatePassengerRequestDto
} from '@/types/api';
import {
  formatDateTime,
  formatSessionStatus,
  formatWeight,
  getSessionStatusColor
} from '@/utils/format';
import { canManageSimulation } from '@/utils/roles';
import { extractErrorMessage } from '@/utils/errors';
import { buildDashboardUrl, buildReportUrl, getQueryParam, updateQueryParam } from '@/utils/urls';

const { currentUser, loading, accessError, initialize } = usePageAccess();
const { snackbar, showError, showSuccess, showWarning } = useSnackbar();

const sessions = ref<SessionListItemDto[]>([]);
const selectedSnapshot = ref<SessionSnapshotDto | null>(null);

const loadingSessions = ref(false);
const loadingSnapshot = ref(false);
const busyAction = ref(false);

const passengerDialogVisible = ref(false);
const editedPassenger = ref<PassengerDto | null>(null);

const sessionToDeleteId = ref<string | null>(null);
const sessionDeleteDialogVisible = ref(false);

const passengerToDelete = ref<PassengerDto | null>(null);
const passengerDeleteDialogVisible = ref(false);

const sessionCreatorRef = ref<InstanceType<typeof SessionCreatorCard> | null>(null);

const canManage = computed(() => canManageSimulation(currentUser.value));

const canIssueGo = computed(() => {
  if (!selectedSnapshot.value || !canManage.value) {
    return false;
  }

  return selectedSnapshot.value.status === 'Running'
    && selectedSnapshot.value.elevator.awaitingGoCommand;
});

const selectedSessionRow = computed(() => {
  if (!selectedSnapshot.value) {
    return null;
  }

  return sessions.value.find((session) => session.id === selectedSnapshot.value!.sessionId) ?? null;
});

function resetTransientState(): void {
  passengerDialogVisible.value = false;
  editedPassenger.value = null;

  sessionToDeleteId.value = null;
  sessionDeleteDialogVisible.value = false;

  passengerToDelete.value = null;
  passengerDeleteDialogVisible.value = false;
}

function resolveTargetSessionId(preferredSessionId?: string | null): string | null {
  const candidateSessionIds = [
    preferredSessionId,
    getQueryParam('sessionId'),
    selectedSnapshot.value?.sessionId,
    sessions.value[0]?.id ?? null
  ];

  for (const candidateSessionId of candidateSessionIds) {
    if (candidateSessionId && sessions.value.some((session) => session.id === candidateSessionId)) {
      return candidateSessionId;
    }
  }

  return null;
}

async function loadSessions(preferredSessionId?: string | null): Promise<void> {
  loadingSessions.value = true;

  try {
    sessions.value = await sessionService.getAllSessions();

    const targetSessionId = resolveTargetSessionId(preferredSessionId);

    if (targetSessionId) {
      await loadSnapshot(targetSessionId);
    } else {
      selectedSnapshot.value = null;
      updateQueryParam('sessionId', null);
    }
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    loadingSessions.value = false;
  }
}

async function loadSnapshot(sessionId: string): Promise<void> {
  loadingSnapshot.value = true;

  try {
    selectedSnapshot.value = await sessionService.getSessionSnapshot(sessionId);
    updateQueryParam('sessionId', sessionId);
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    loadingSnapshot.value = false;
  }
}

async function handleRefresh(): Promise<void> {
  resetTransientState();
  sessionCreatorRef.value?.reset();

  await loadSessions(selectedSnapshot.value?.sessionId ?? getQueryParam('sessionId'));
}

async function createSession(request: CreateSessionRequestDto): Promise<void> {
  busyAction.value = true;

  try {
    const snapshot = await sessionService.createSession(request);

    selectedSnapshot.value = snapshot;
    updateQueryParam('sessionId', snapshot.sessionId);
    await loadSessions(snapshot.sessionId);

    sessionCreatorRef.value?.reset();
    showSuccess('Сеанс успешно создан.');
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
  }
}

async function startSession(sessionId: string): Promise<void> {
  busyAction.value = true;

  try {
    const snapshot = await sessionService.startSession(sessionId);

    selectedSnapshot.value = snapshot;
    await loadSessions(snapshot.sessionId);

    showSuccess('Сеанс успешно запущен.');
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
  }
}

async function issueGoCommand(): Promise<void> {
  if (!selectedSnapshot.value) {
    return;
  }

  busyAction.value = true;

  try {
    selectedSnapshot.value = await sessionService.issueGoCommand(selectedSnapshot.value.sessionId);
    await loadSessions(selectedSnapshot.value.sessionId);

    showSuccess('Команда «Ход» передана.');
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
  }
}

async function stopSession(sessionId: string): Promise<void> {
  busyAction.value = true;

  try {
    const snapshot = await sessionService.stopSession(sessionId);

    selectedSnapshot.value = snapshot;
    await loadSessions(snapshot.sessionId);

    if (snapshot.status === 'Stopped') {
      showSuccess('Сеанс остановлен.');
    } else {
      showWarning('Остановка запрошена. Сеанс будет завершен при допустимом состоянии.');
    }
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
  }
}

function requestDeleteSession(sessionId: string): void {
  sessionToDeleteId.value = sessionId;
  sessionDeleteDialogVisible.value = true;
}

async function confirmDeleteSession(): Promise<void> {
  if (!sessionToDeleteId.value) {
    return;
  }

  busyAction.value = true;

  try {
    const deletedSessionId = sessionToDeleteId.value;

    await sessionService.deleteSession(deletedSessionId);

    if (selectedSnapshot.value?.sessionId === deletedSessionId) {
      selectedSnapshot.value = null;
      updateQueryParam('sessionId', null);
    }

    await loadSessions();
    showSuccess('Сеанс удален.');
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
    sessionToDeleteId.value = null;
  }
}

function openCreatePassengerDialog(): void {
  editedPassenger.value = null;
  passengerDialogVisible.value = true;
}

function openEditPassengerDialog(passenger: PassengerDto): void {
  editedPassenger.value = passenger;
  passengerDialogVisible.value = true;
}

async function savePassenger(
  request: CreatePassengerRequestDto | UpdatePassengerRequestDto
): Promise<void> {
  if (!selectedSnapshot.value) {
    return;
  }

  busyAction.value = true;

  try {
    if (editedPassenger.value) {
      selectedSnapshot.value = await sessionService.updatePassenger(
        selectedSnapshot.value.sessionId,
        editedPassenger.value.id,
        request
      );

      showSuccess('Параметры пассажира обновлены.');
    } else {
      selectedSnapshot.value = await sessionService.createPassenger(
        selectedSnapshot.value.sessionId,
        request
      );

      showSuccess('Пассажир добавлен в сеанс.');
    }

    passengerDialogVisible.value = false;
    editedPassenger.value = null;

    await loadSessions(selectedSnapshot.value.sessionId);
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
  }
}

function requestDeletePassenger(passenger: PassengerDto): void {
  passengerToDelete.value = passenger;
  passengerDeleteDialogVisible.value = true;
}

async function confirmDeletePassenger(): Promise<void> {
  if (!selectedSnapshot.value || !passengerToDelete.value) {
    return;
  }

  busyAction.value = true;

  try {
    selectedSnapshot.value = await sessionService.deletePassenger(
      selectedSnapshot.value.sessionId,
      passengerToDelete.value.id
    );

    await loadSessions(selectedSnapshot.value.sessionId);
    showSuccess('Пассажир удален из черновика.');
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
    passengerToDelete.value = null;
  }
}

async function handleLogout(): Promise<void> {
  await authService.logout();
  window.location.href = '/';
}

onMounted(async () => {
  await initialize();

  if (accessError.value) {
    return;
  }

  await loadSessions();
});
</script>

<template>
  <AppShell
    page-title="Управление сеансами"
    active-page="sessions"
    :current-user="currentUser"
    :status-bar="selectedSnapshot?.statusBar"
    @logout="handleLogout"
  >
    <template #toolbar>
      <v-btn
        variant="tonal"
        color="primary"
        prepend-icon="mdi-refresh"
        :loading="loadingSessions || loadingSnapshot"
        @click="handleRefresh"
      >
        Обновить
      </v-btn>
    </template>

    <template v-if="loading">
      <LoadingPanel text="Проверка прав доступа и загрузка страницы..." />
    </template>

    <template v-else-if="accessError">
      <EmptyState
        title="Недостаточно прав"
        description="У текущего пользователя нет доступа к этой странице."
        icon="mdi-shield-alert-outline"
        action-text="Перейти к мониторингу"
        action-href="/dashboard/"
      />
    </template>

    <template v-else>
      <v-row class="mb-6" v-if="canManage">
        <v-col cols="12">
          <SessionCreatorCard
            ref="sessionCreatorRef"
            :busy="busyAction"
            @create="createSession"
          />
        </v-col>
      </v-row>

      <v-row class="mb-6">
        <v-col cols="12">
          <SectionCard
            title="Список сеансов"
            subtitle="Выберите сеанс для просмотра или управления его жизненным циклом."
          >
            <SessionsTable
              :sessions="sessions"
              :selected-session-id="selectedSnapshot?.sessionId"
              :can-manage="canManage"
              @select="loadSnapshot"
              @start="startSession"
              @stop="stopSession"
              @delete="requestDeleteSession"
            />
          </SectionCard>
        </v-col>
      </v-row>

      <template v-if="selectedSnapshot">
        <v-row class="mb-6">
          <v-col cols="12" md="6" xl="3">
            <StatTile
              title="Название сеанса"
              :value="selectedSnapshot.sessionName"
              icon="mdi-file-document-outline"
              color="primary"
            />
          </v-col>

          <v-col cols="12" md="6" xl="3">
            <StatTile
              title="Количество этажей"
              :value="selectedSnapshot.floorCount"
              icon="mdi-office-building-outline"
              color="accent"
            />
          </v-col>

          <v-col cols="12" md="6" xl="3">
            <StatTile
              title="Текущий статус"
              :value="formatSessionStatus(selectedSnapshot.status)"
              icon="mdi-timeline-check"
              :color="getSessionStatusColor(selectedSnapshot.status)"
            />
          </v-col>

          <v-col cols="12" md="6" xl="3">
            <StatTile
              title="Текущая нагрузка"
              :value="formatWeight(selectedSnapshot.elevator.currentLoadKg)"
              icon="mdi-weight-kilogram"
              color="warning"
            />
          </v-col>
        </v-row>

        <v-row class="mb-6">
          <v-col cols="12">
            <SectionCard
              title="Сведения о выбранном сеансе"
              subtitle="Карточка текущего выбранного сеанса с быстрыми действиями."
            >
              <template #actions>
                <v-btn
                  v-if="selectedSnapshot.status === 'Draft' && canManage"
                  color="success"
                  variant="flat"
                  prepend-icon="mdi-play-circle-outline"
                  :loading="busyAction"
                  @click="startSession(selectedSnapshot.sessionId)"
                >
                  Запустить
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
                  v-if="selectedSnapshot.status === 'Running' && canManage"
                  color="warning"
                  variant="flat"
                  prepend-icon="mdi-stop-circle-outline"
                  :loading="busyAction"
                  @click="stopSession(selectedSnapshot.sessionId)"
                >
                  Остановить
                </v-btn>

                <v-btn
                  variant="tonal"
                  color="info"
                  prepend-icon="mdi-monitor-dashboard"
                  :href="buildDashboardUrl(selectedSnapshot.sessionId)"
                >
                  Мониторинг
                </v-btn>

                <v-btn
                  v-if="selectedSnapshot.status === 'Stopped'"
                  variant="tonal"
                  color="secondary"
                  prepend-icon="mdi-file-chart"
                  :href="buildReportUrl(selectedSnapshot.sessionId)"
                >
                  Отчёт
                </v-btn>
              </template>

              <v-alert
                v-if="selectedSnapshot.elevator.awaitingGoCommand"
                type="info"
                variant="tonal"
                class="mb-4"
              >
                В выбранном сеансе накоплены активные заявки на перемещение.
                Для начала движения нажмите кнопку «Ход».
              </v-alert>

              <v-row>
                <v-col cols="12" md="4">
                  <div class="text-body-2 text-medium-emphasis">Создан</div>
                  <div class="text-body-1 font-weight-medium mt-1">
                    {{ formatDateTime(selectedSessionRow?.createdAtUtc) }}
                  </div>
                </v-col>

                <v-col cols="12" md="4">
                  <div class="text-body-2 text-medium-emphasis">Запущен</div>
                  <div class="text-body-1 font-weight-medium mt-1">
                    {{ formatDateTime(selectedSessionRow?.startedAtUtc) }}
                  </div>
                </v-col>

                <v-col cols="12" md="4">
                  <div class="text-body-2 text-medium-emphasis">Остановлен</div>
                  <div class="text-body-1 font-weight-medium mt-1">
                    {{ formatDateTime(selectedSessionRow?.stoppedAtUtc) }}
                  </div>
                </v-col>
              </v-row>
            </SectionCard>
          </v-col>
        </v-row>

        <v-row>
          <v-col cols="12">
            <SectionCard
              title="Пассажиры выбранного сеанса"
              subtitle="Для черновика доступно добавление, редактирование и удаление пассажиров."
            >
              <template #actions>
                <v-btn
                  v-if="selectedSnapshot.status === 'Draft' && canManage"
                  color="primary"
                  variant="flat"
                  prepend-icon="mdi-account-plus"
                  @click="openCreatePassengerDialog"
                >
                  Добавить пассажира
                </v-btn>
              </template>

              <v-alert
                v-if="selectedSnapshot.status !== 'Draft'"
                type="info"
                variant="tonal"
                class="mb-4"
              >
                Редактирование списка пассажиров доступно только для сеанса в состоянии
                «Черновик».
              </v-alert>

              <DraftPassengerTable
                :passengers="selectedSnapshot.passengers"
                :editable="selectedSnapshot.status === 'Draft' && canManage"
                @edit="openEditPassengerDialog"
                @delete="requestDeletePassenger"
              />
            </SectionCard>
          </v-col>
        </v-row>
      </template>

      <template v-else>
        <EmptyState
          title="Сеанс не выбран"
          description="Создайте новый сеанс или выберите существующий сеанс из таблицы выше."
          icon="mdi-calendar-question-outline"
        />
      </template>
    </template>

    <PassengerFormDialog
      v-model="passengerDialogVisible"
      :title="editedPassenger ? 'Редактирование пассажира' : 'Добавление пассажира'"
      :floor-count="selectedSnapshot?.floorCount ?? 2"
      :passenger="editedPassenger"
      :busy="busyAction"
      @save="savePassenger"
    />

    <ConfirmDialog
      v-model="sessionDeleteDialogVisible"
      title="Удаление сеанса"
      text="Вы уверены, что хотите удалить выбранный сеанс? Операция необратима."
      confirm-text="Удалить"
      @confirm="confirmDeleteSession"
    />

    <ConfirmDialog
      v-model="passengerDeleteDialogVisible"
      title="Удаление пассажира"
      text="Удалить выбранного пассажира из черновика сеанса?"
      confirm-text="Удалить"
      @confirm="confirmDeletePassenger"
    />

    <GlobalSnackbar
      v-model="snackbar.visible"
      :text="snackbar.text"
      :color="snackbar.color"
      :timeout="snackbar.timeout"
    />
  </AppShell>
</template>
