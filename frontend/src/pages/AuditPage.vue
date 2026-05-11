<script setup lang="ts">
import { onMounted, ref } from 'vue';
import AppShell from '@/components/common/AppShell.vue';
import EmptyState from '@/components/common/EmptyState.vue';
import GlobalSnackbar from '@/components/common/GlobalSnackbar.vue';
import LoadingPanel from '@/components/common/LoadingPanel.vue';
import AuditFiltersCard from '@/components/audit/AuditFiltersCard.vue';
import AuditTable from '@/components/audit/AuditTable.vue';
import SectionCard from '@/components/common/SectionCard.vue';
import { useOptionalStatusBar } from '@/composables/useOptionalStatusBar';
import { usePageAccess } from '@/composables/usePageAccess';
import { useSnackbar } from '@/composables/useSnackbar';
import { auditService } from '@/services/audit-service';
import { authService } from '@/services/auth-service';
import type { AuditLogDto, AuditLogQueryDto } from '@/types/api';
import { extractErrorMessage } from '@/utils/errors';

type AuditFiltersCardExposed = {
  getCurrentQuery: () => AuditLogQueryDto;
  resetFilters: () => void;
};

const { currentUser, loading, accessError, initialize } = usePageAccess(['Admin']);
const { statusBar, refreshStatusBar } = useOptionalStatusBar();
const { snackbar, showError } = useSnackbar();

const filtersCardRef = ref<AuditFiltersCardExposed | null>(null);

const logs = ref<AuditLogDto[]>([]);
const initialLoading = ref(true);
const loadingLogs = ref(false);
const lastQuery = ref<AuditLogQueryDto>({
  limit: 200
});

async function loadLogs(query: AuditLogQueryDto = lastQuery.value): Promise<void> {
  loadingLogs.value = true;
  lastQuery.value = { ...query };

  try {
    logs.value = await auditService.getAuditLogs(query);
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    loadingLogs.value = false;
  }
}

async function refreshPage(): Promise<void> {
  const currentQuery = filtersCardRef.value?.getCurrentQuery() ?? lastQuery.value;

  await Promise.all([
    loadLogs(currentQuery),
    refreshStatusBar()
  ]);
}

async function handleLogout(): Promise<void> {
  await authService.logout();
  window.location.href = '/';
}

onMounted(async () => {
  await initialize();

  if (accessError.value) {
    initialLoading.value = false;
    return;
  }

  try {
    await Promise.all([
      loadLogs({
        limit: 200
      }),
      refreshStatusBar()
    ]);
  } finally {
    initialLoading.value = false;
  }
});
</script>

<template>
  <AppShell
    page-title="Журнал аудита"
    active-page="audit"
    :current-user="currentUser"
    :status-bar="statusBar"
    @logout="handleLogout"
  >
    <template #toolbar>
      <v-btn
        variant="tonal"
        color="primary"
        prepend-icon="mdi-refresh"
        :loading="loadingLogs"
        @click="refreshPage"
      >
        Обновить
      </v-btn>
    </template>

    <template v-if="loading || initialLoading">
      <LoadingPanel text="Загрузка журнала аудита..." />
    </template>

    <template v-else-if="accessError">
      <EmptyState
        title="Недостаточно прав"
        description="Журнал аудита доступен только администраторам."
        icon="mdi-shield-alert-outline"
        action-text="К мониторингу"
        action-href="/dashboard/"
      />
    </template>

    <template v-else>
      <v-row class="mb-6">
        <v-col cols="12">
          <AuditFiltersCard
            ref="filtersCardRef"
            :busy="loadingLogs"
            @apply="loadLogs"
          />
        </v-col>
      </v-row>

      <v-row>
        <v-col cols="12">
          <SectionCard
            title="Записи журнала аудита"
            subtitle="Журнал отражает действия пользователей и используется для задач мониторинга и безопасности."
          >
            <v-progress-linear
              v-if="loadingLogs"
              indeterminate
              color="primary"
              class="mb-4"
            ></v-progress-linear>

            <AuditTable :logs="logs" />
          </SectionCard>
        </v-col>
      </v-row>
    </template>

    <GlobalSnackbar
      v-model="snackbar.visible"
      :text="snackbar.text"
      :color="snackbar.color"
      :timeout="snackbar.timeout"
    />
  </AppShell>
</template>
