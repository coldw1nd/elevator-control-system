<script setup lang="ts">
import type { AuditLogDto } from '@/types/api';
import { formatDateTime } from '@/utils/format';

defineProps<{
  logs: AuditLogDto[];
}>();
</script>

<template>
  <v-table density="comfortable" class="rounded-lg border">
    <thead>
      <tr>
        <th>Время</th>
        <th>Пользователь</th>
        <th>Действие</th>
        <th>Сущность</th>
        <th>EntityId</th>
        <th>IP</th>
        <th>Описание</th>
      </tr>
    </thead>

    <tbody>
      <tr v-for="log in logs" :key="log.id">
        <td>{{ formatDateTime(log.createdAtUtc) }}</td>
        <td>{{ log.username }}</td>
        <td>{{ log.action }}</td>
        <td>{{ log.entityType }}</td>
        <td>{{ log.entityId ?? '—' }}</td>
        <td>{{ log.ipAddress || '—' }}</td>
        <td class="audit-details-cell">{{ log.details }}</td>
      </tr>

      <tr v-if="logs.length === 0">
        <td colspan="7" class="text-center text-medium-emphasis py-6">
          Записи аудита не найдены.
        </td>
      </tr>
    </tbody>
  </v-table>
</template>

<style scoped>
.audit-details-cell {
  max-width: 420px;
  white-space: normal;
}
</style>
