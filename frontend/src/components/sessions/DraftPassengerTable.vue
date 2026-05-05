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
  editable: boolean;
}>();

defineEmits<{
  edit: [passenger: PassengerDto];
  delete: [passenger: PassengerDto];
}>();
</script>

<template>
  <v-table density="comfortable" class="rounded-lg border">
    <thead>
      <tr>
        <th>Вес</th>
        <th>Откуда</th>
        <th>Куда</th>
        <th>Статус</th>
        <th>Создан</th>
        <th class="text-right">Действия</th>
      </tr>
    </thead>

    <tbody>
      <tr v-for="passenger in passengers" :key="passenger.id">
        <td>{{ formatWeight(passenger.weightKg) }}</td>
        <td>{{ passenger.sourceFloor }}</td>
        <td>{{ passenger.targetFloor }}</td>
        <td>
          <v-chip
            size="small"
            :color="getPassengerStatusColor(passenger.status)"
            variant="tonal"
          >
            {{ formatPassengerStatus(passenger.status) }}
          </v-chip>
        </td>
        <td>{{ formatDateTime(passenger.createdAtUtc) }}</td>
        <td class="text-right">
          <template v-if="editable">
            <v-btn
              icon="mdi-pencil-outline"
              variant="text"
              color="primary"
              @click="$emit('edit', passenger)"
            ></v-btn>
            <v-btn
              icon="mdi-delete-outline"
              variant="text"
              color="error"
              @click="$emit('delete', passenger)"
            ></v-btn>
          </template>

          <template v-else>
            <span class="text-medium-emphasis">Только просмотр</span>
          </template>
        </td>
      </tr>

      <tr v-if="passengers.length === 0">
        <td colspan="6" class="text-center text-medium-emphasis py-6">
          В выбранном сеансе пока нет пассажиров.
        </td>
      </tr>
    </tbody>
  </v-table>
</template>
