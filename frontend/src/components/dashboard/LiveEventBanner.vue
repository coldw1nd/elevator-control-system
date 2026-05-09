<script setup lang="ts">
import { computed } from 'vue';
import type { SimulationConnectionStatus } from '@/types/api';
import { formatDateTime } from '@/utils/format';

const props = defineProps<{
  status: SimulationConnectionStatus;
  updatedAt?: string | null;
}>();

const color = computed(() => {
  switch (props.status) {
    case 'connected':
      return 'success';
    case 'connecting':
      return 'info';
    case 'reconnecting':
      return 'warning';
    case 'disconnected':
      return 'error';
    default:
      return 'info';
  }
});

const icon = computed(() => {
  switch (props.status) {
    case 'connected':
      return 'mdi-lan-connect';
    case 'connecting':
      return 'mdi-lan-pending';
    case 'reconnecting':
      return 'mdi-restore-alert';
    case 'disconnected':
      return 'mdi-lan-disconnect';
    default:
      return 'mdi-help-circle-outline';
  }
});

const text = computed(() => {
  switch (props.status) {
    case 'connected':
      return 'Соединение с сервером событий установлено.';
    case 'connecting':
      return 'Выполняется подключение к серверу событий.';
    case 'reconnecting':
      return 'Соединение потеряно, выполняется переподключение.';
    case 'disconnected':
      return 'Соединение с сервером событий отсутствует.';
    default:
      return 'Состояние соединения неизвестно.';
  }
});
</script>

<template>
  <v-alert :type="color" variant="tonal" :icon="icon">
    <div class="d-flex align-center justify-space-between flex-wrap ga-2">
      <span>{{ text }}</span>

      <span v-if="updatedAt" class="text-caption">
        Последнее обновление: {{ formatDateTime(updatedAt) }}
      </span>
    </div>
  </v-alert>
</template>
