<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type {
  CreatePassengerRequestDto,
  PassengerDto,
  UpdatePassengerRequestDto
} from '@/types/api';

const visible = defineModel<boolean>({
  required: true
});

const props = withDefaults(
  defineProps<{
    title: string;
    floorCount: number;
    passenger?: PassengerDto | null;
    defaultSourceFloor?: number;
    busy?: boolean;
  }>(),
  {
    passenger: null,
    defaultSourceFloor: 1,
    busy: false
  }
);

const emit = defineEmits<{
  save: [request: CreatePassengerRequestDto | UpdatePassengerRequestDto];
}>();

const weightKg = ref(70);
const sourceFloor = ref(1);
const targetFloor = ref(2);

function syncForm(): void {
  if (props.passenger) {
    weightKg.value = props.passenger.weightKg;
    sourceFloor.value = props.passenger.sourceFloor;
    targetFloor.value = props.passenger.targetFloor;
    return;
  }

  weightKg.value = 70;
  sourceFloor.value = Math.min(Math.max(Math.trunc(props.defaultSourceFloor), 1), props.floorCount);
  targetFloor.value =
    sourceFloor.value === 1 ? Math.min(2, props.floorCount) : 1;
}

watch(
  () => [visible.value, props.passenger, props.floorCount, props.defaultSourceFloor],
  () => {
    if (visible.value) {
      syncForm();
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
    return 'Этаж появления выходит за пределы числа этажей.';
  }

  if (normalizedTargetFloor < 1 || normalizedTargetFloor > props.floorCount) {
    return 'Целевой этаж выходит за пределы числа этажей.';
  }

  if (normalizedSourceFloor === normalizedTargetFloor) {
    return 'Этаж появления и целевой этаж не должны совпадать.';
  }

  return '';
});

const canSave = computed(() => validationMessage.value.length === 0 && !props.busy);

function handleSave(): void {
  if (!canSave.value) {
    return;
  }

  emit('save', {
    weightKg: Number(weightKg.value),
    sourceFloor: Math.trunc(Number(sourceFloor.value)),
    targetFloor: Math.trunc(Number(targetFloor.value))
  });
}
</script>

<template>
  <v-dialog v-model="visible" :persistent="props.busy">
    <v-card>
      <v-card-title class="text-h6">{{ title }}</v-card-title>

      <v-card-text class="d-flex flex-column ga-4">
        <v-text-field
          v-model.number="weightKg"
          label="Вес пассажира, кг"
          type="number"
          min="20"
          max="400"
          step="0.1"
          :disabled="props.busy"
        ></v-text-field>

        <v-text-field
          v-model.number="sourceFloor"
          label="Этаж появления"
          type="number"
          min="1"
          :max="floorCount"
          step="1"
          :disabled="props.busy"
        ></v-text-field>

        <v-text-field
          v-model.number="targetFloor"
          label="Целевой этаж"
          type="number"
          min="1"
          :max="floorCount"
          step="1"
          :disabled="props.busy"
        ></v-text-field>

        <v-alert v-if="validationMessage" type="warning" variant="tonal">
          {{ validationMessage }}
        </v-alert>
      </v-card-text>

      <v-card-actions class="justify-end">
        <v-btn variant="text" :disabled="props.busy" @click="visible = false">Отмена</v-btn>
        <v-btn
          color="primary"
          variant="flat"
          :disabled="!canSave"
          :loading="props.busy"
          @click="handleSave"
        >
          Сохранить
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
