<script setup lang="ts">
import type { PassengerDto } from '@/types/api';
import {
  formatDateTime,
  formatPassengerStatus,
  formatWeight,
  getPassengerStatusColor
} from '@/utils/format';

defineProps<{
  passengers: PassengerDto[];
  locatingPassengerId?: string | null;
  allowLocate: boolean;
}>();

defineEmits<{
  locate: [passengerId: string];
}>();
</script>

<template>
  <v-table density="comfortable" class="rounded-lg border">
    <thead>
      <tr>
        <th>Вес</th>
        <th>Маршрут</th>
        <th>Текущий этаж</th>
        <th>Статус</th>
        <th>Положение</th>
        <th>Создан</th>
        <th v-if="allowLocate" class="text-right">Действия</th>
      </tr>
    </thead>

    <tbody>
      <tr v-for="passenger in passengers" :key="passenger.id">
        <td>{{ formatWeight(passenger.weightKg) }}</td>
        <td>{{ passenger.sourceFloor }} → {{ passenger.targetFloor }}</td>
        <td>{{ passenger.currentFloor }}</td>
        <td>
          <v-chip
            size="small"
            :color="getPassengerStatusColor(passenger.status)"
            variant="tonal"
          >
            {{ formatPassengerStatus(passenger.status) }}
          </v-chip>
        </td>
        <td class="passenger-location-cell">
          {{ passenger.locationDescription }}
        </td>
        <td>{{ formatDateTime(passenger.createdAtUtc) }}</td>
        <td v-if="allowLocate" class="text-right">
          <v-btn
            size="small"
            variant="text"
            color="info"
            prepend-icon="mdi-crosshairs-gps"
            :loading="locatingPassengerId === passenger.id"
            @click="$emit('locate', passenger.id)"
          >
            Опросить
          </v-btn>
        </td>
      </tr>

      <tr v-if="passengers.length === 0">
        <td :colspan="allowLocate ? 7 : 6" class="text-center text-medium-emphasis py-6">
          Пассажиры отсутствуют.
        </td>
      </tr>
    </tbody>
  </v-table>
</template>

<style scoped>
.passenger-location-cell {
  max-width: 320px;
  white-space: normal;
}
</style>
