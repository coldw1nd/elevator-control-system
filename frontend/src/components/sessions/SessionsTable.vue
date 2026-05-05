<script setup lang="ts">
import type { SessionListItemDto } from '@/types/api';
import {
  formatDateTime,
  formatFloorCount,
  formatSessionStatus,
  getSessionStatusColor
} from '@/utils/format';
import { buildDashboardUrl, buildReportUrl } from '@/utils/urls';

defineProps<{
  sessions: SessionListItemDto[];
  selectedSessionId?: string | null;
  canManage: boolean;
}>();

defineEmits<{
  select: [sessionId: string];
  start: [sessionId: string];
  stop: [sessionId: string];
  delete: [sessionId: string];
}>();
</script>

<template>
  <v-table density="comfortable" class="rounded-lg border">
    <thead>
      <tr>
        <th>Название</th>
        <th>Этажи</th>
        <th>Статус</th>
        <th>Пассажиры</th>
        <th>Создан</th>
        <th>Запущен</th>
        <th>Остановлен</th>
        <th class="text-right">Действия</th>
      </tr>
    </thead>

    <tbody>
      <tr
        v-for="session in sessions"
        :key="session.id"
        :class="{ 'table-row-selected': session.id === selectedSessionId }"
        @click="$emit('select', session.id)"
      >
        <td>{{ session.name }}</td>
        <td>{{ formatFloorCount(session.floorCount) }}</td>
        <td>
          <v-chip
            size="small"
            :color="getSessionStatusColor(session.status)"
            variant="tonal"
          >
            {{ formatSessionStatus(session.status) }}
          </v-chip>
        </td>
        <td>{{ session.totalPassengers }}</td>
        <td>{{ formatDateTime(session.createdAtUtc) }}</td>
        <td>{{ formatDateTime(session.startedAtUtc) }}</td>
        <td>{{ formatDateTime(session.stoppedAtUtc) }}</td>
        <td class="text-right">
          <div class="d-flex justify-end ga-1 flex-wrap">
            <v-btn
              size="small"
              variant="text"
              color="primary"
              prepend-icon="mdi-eye-outline"
              @click.stop="$emit('select', session.id)"
            >
              Выбрать
            </v-btn>

            <v-btn
              size="small"
              variant="text"
              color="info"
              prepend-icon="mdi-monitor-dashboard"
              :href="buildDashboardUrl(session.id)"
              @click.stop
            >
              Мониторинг
            </v-btn>

            <v-btn
              v-if="session.hasReport"
              size="small"
              variant="text"
              color="secondary"
              prepend-icon="mdi-file-chart"
              :href="buildReportUrl(session.id)"
              @click.stop
            >
              Отчёт
            </v-btn>

            <v-btn
              v-if="canManage && session.status === 'Draft'"
              size="small"
              variant="text"
              color="success"
              prepend-icon="mdi-play-circle-outline"
              @click.stop="$emit('start', session.id)"
            >
              Старт
            </v-btn>

            <v-btn
              v-if="canManage && session.status === 'Running'"
              size="small"
              variant="text"
              color="warning"
              prepend-icon="mdi-stop-circle-outline"
              @click.stop="$emit('stop', session.id)"
            >
              Стоп
            </v-btn>

            <v-btn
              v-if="canManage && session.status !== 'Running'"
              size="small"
              variant="text"
              color="error"
              prepend-icon="mdi-delete-outline"
              @click.stop="$emit('delete', session.id)"
            >
              Удалить
            </v-btn>
          </div>
        </td>
      </tr>

      <tr v-if="sessions.length === 0">
        <td colspan="8" class="text-center text-medium-emphasis py-6">
          Сеансы пока не созданы.
        </td>
      </tr>
    </tbody>
  </v-table>
</template>

<style scoped>
.table-row-selected {
  background: rgba(37, 99, 235, 0.06);
}
</style>
