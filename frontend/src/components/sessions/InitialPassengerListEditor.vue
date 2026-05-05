<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { CreatePassengerRequestDto } from '@/types/api';
import { formatWeight } from '@/utils/format';

const props = defineProps<{
  modelValue: CreatePassengerRequestDto[];
  floorCount: number;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: CreatePassengerRequestDto[]];
}>();

const weightKg = ref(70);
const sourceFloor = ref(1);
const targetFloor = ref(2);

watch(
  () => props.floorCount,
  (newFloorCount) => {
    sourceFloor.value = Math.min(Math.max(Math.trunc(Number(sourceFloor.value)), 1), newFloorCount);
    targetFloor.value = Math.min(Math.max(Math.trunc(Number(targetFloor.value)), 1), newFloorCount);

    if (sourceFloor.value === targetFloor.value) {
      targetFloor.value = sourceFloor.value === 1 ? Math.min(2, newFloorCount) : 1;
    }
  },
  {
    immediate: true
  }
);

const validationMessage = computed(() => {
  const normalizedWeight = Number(weightKg.value);
  const normalizedSourceFloor = Number(sourceFloor.value);
  const normalizedTargetFloor = Number(targetFloor.value);

  if (!Number.isFinite(normalizedWeight) || normalizedWeight < 20 || normalizedWeight > 400) {
    return 'Вес пассажира должен быть в диапазоне от 20 до 400 кг.';
  }

  if (!Number.isInteger(normalizedSourceFloor)) {
    return 'Этаж появления должен быть целым числом.';
  }

  if (!Number.isInteger(normalizedTargetFloor)) {
    return 'Целевой этаж должен быть целым числом.';
  }

  if (normalizedSourceFloor < 1 || normalizedSourceFloor > props.floorCount) {
    return 'Этаж появления выходит за пределы количества этажей.';
  }

  if (normalizedTargetFloor < 1 || normalizedTargetFloor > props.floorCount) {
    return 'Целевой этаж выходит за пределы количества этажей.';
  }

  if (normalizedSourceFloor === normalizedTargetFloor) {
    return 'Этаж появления и целевой этаж не должны совпадать.';
  }

  return '';
});

const canAddPassenger = computed(() => validationMessage.value.length === 0);

function addPassenger(): void {
  if (!canAddPassenger.value) {
    return;
  }

  const nextPassengers = [
    ...props.modelValue,
    {
      weightKg: Number(weightKg.value),
      sourceFloor: Math.trunc(Number(sourceFloor.value)),
      targetFloor: Math.trunc(Number(targetFloor.value))
    }
  ];

  emit('update:modelValue', nextPassengers);

  weightKg.value = 70;
  sourceFloor.value = 1;
  targetFloor.value = props.floorCount >= 2 ? 2 : 1;
}

function removePassenger(index: number): void {
  const nextPassengers = props.modelValue.filter((_, currentIndex) => currentIndex !== index);

  emit('update:modelValue', nextPassengers);
}
</script>

<template>
  <div class="d-flex flex-column ga-4">
    <div class="text-body-2 text-medium-emphasis">
      Добавьте стартовых пассажиров. При необходимости новых пассажиров можно будет создавать
      позже, в ходе сеанса.
    </div>

    <v-row>
      <v-col cols="12" md="4">
        <v-text-field
          v-model.number="weightKg"
          label="Вес пассажира, кг"
          type="number"
          min="20"
          max="400"
          step="0.1"
        ></v-text-field>
      </v-col>

      <v-col cols="12" md="4">
        <v-text-field
          v-model.number="sourceFloor"
          label="Этаж появления"
          type="number"
          min="1"
          :max="floorCount"
          step="1"
        ></v-text-field>
      </v-col>

      <v-col cols="12" md="4">
        <v-text-field
          v-model.number="targetFloor"
          label="Целевой этаж"
          type="number"
          min="1"
          :max="floorCount"
          step="1"
        ></v-text-field>
      </v-col>
    </v-row>

    <v-alert v-if="validationMessage" type="warning" variant="tonal">
      {{ validationMessage }}
    </v-alert>

    <div class="d-flex justify-end">
      <v-btn
        color="primary"
        variant="tonal"
        prepend-icon="mdi-account-plus"
        :disabled="!canAddPassenger"
        @click="addPassenger"
      >
        Добавить пассажира
      </v-btn>
    </div>

    <div v-if="modelValue.length === 0" class="text-body-2 text-medium-emphasis">
      Стартовый список пассажиров пока пуст.
    </div>

    <v-table v-else density="comfortable" class="rounded-lg border">
      <thead>
        <tr>
          <th>#</th>
          <th>Вес</th>
          <th>Этаж появления</th>
          <th>Целевой этаж</th>
          <th class="text-right">Действия</th>
        </tr>
      </thead>

      <tbody>
        <tr v-for="(passenger, index) in modelValue" :key="`${index}-${passenger.sourceFloor}`">
          <td>{{ index + 1 }}</td>
          <td>{{ formatWeight(passenger.weightKg) }}</td>
          <td>{{ passenger.sourceFloor }}</td>
          <td>{{ passenger.targetFloor }}</td>
          <td class="text-right">
            <v-btn
              color="error"
              variant="text"
              icon="mdi-delete-outline"
              @click="removePassenger(index)"
            ></v-btn>
          </td>
        </tr>
      </tbody>
    </v-table>
  </div>
</template>
