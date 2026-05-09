<script setup lang="ts">
import type { SessionListItemDto } from '@/types/api';
import {
  formatDateTime,
  formatFloorCount,
  formatSessionStatus,
  getSessionStatusColor
} from '@/utils/format';
import { buildDashboardUrl } from '@/utils/urls';

defineProps<{
  sessions: SessionListItemDto[];
  selectedSessionId?: string | null;
}>();

defineEmits<{
  select: [sessionId: string];
}>();
</script>

<template>
  <v-table density="comfortable" class="rounded-lg border">
    <thead>
      <tr>
        <th>Название</th>
        <th>Статус</th>
        <th>Этажи</th>
        <th>Пассажиры</th>
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
        <td>
          <v-chip
            size="small"
            :color="getSessionStatusColor(session.status)"
            variant="tonal"
          >
            {{ formatSessionStatus(session.status) }}
          </v-chip>
        </td>
        <td>{{ formatFloorCount(session.floorCount) }}</td>
        <td>{{ session.totalPassengers }}</td>
        <td>{{ formatDateTime(session.stoppedAtUtc) }}</td>
        <td class="text-right">
          <div class="d-flex justify-end ga-2 flex-wrap">
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
          </div>
        </td>
      </tr>

      <tr v-if="sessions.length === 0">
        <td colspan="6" class="text-center text-medium-emphasis py-6">
          Остановленные сеансы с отчётами отсутствуют.
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
