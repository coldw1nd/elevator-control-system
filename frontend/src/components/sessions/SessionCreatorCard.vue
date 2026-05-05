<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { CreatePassengerRequestDto, CreateSessionRequestDto } from '@/types/api';
import SectionCard from '@/components/common/SectionCard.vue';
import InitialPassengerListEditor from '@/components/sessions/InitialPassengerListEditor.vue';

const props = defineProps<{
  busy: boolean;
}>();

const emit = defineEmits<{
  create: [request: CreateSessionRequestDto];
}>();

const sessionName = ref('');
const floorCount = ref(9);
const initialPassengers = ref<CreatePassengerRequestDto[]>([]);
const passengerAdjustmentMessage = ref('');

function isValidPassengerForFloorCount(
  passenger: CreatePassengerRequestDto,
  currentFloorCount: number
): boolean {
  return (
    Number.isFinite(passenger.weightKg) &&
    passenger.weightKg >= 20 &&
    passenger.weightKg <= 400 &&
    Number.isInteger(passenger.sourceFloor) &&
    Number.isInteger(passenger.targetFloor) &&
    passenger.sourceFloor >= 1 &&
    passenger.sourceFloor <= currentFloorCount &&
    passenger.targetFloor >= 1 &&
    passenger.targetFloor <= currentFloorCount &&
    passenger.sourceFloor !== passenger.targetFloor
  );
}

watch(floorCount, (newValue) => {
  passengerAdjustmentMessage.value = '';

  const numericFloorCount = Number(newValue);

  if (!Number.isFinite(numericFloorCount)) {
    return;
  }

  const normalizedFloorCount = Math.trunc(numericFloorCount);

  if (normalizedFloorCount !== numericFloorCount) {
    floorCount.value = normalizedFloorCount;
    return;
  }

  if (normalizedFloorCount < 2 || normalizedFloorCount > 50) {
    return;
  }

  // Если пользователь уменьшил число этажей, часть уже добавленных пассажиров
  // может стать невалидной. Лучше убрать их сразу на клиенте, чем потом
  // получать отказ от backend при создании сеанса.
  const filteredPassengers = initialPassengers.value.filter((passenger) =>
    isValidPassengerForFloorCount(passenger, normalizedFloorCount)
  );

  if (filteredPassengers.length !== initialPassengers.value.length) {
    initialPassengers.value = filteredPassengers;
    passengerAdjustmentMessage.value =
      'После изменения количества этажей часть стартовых пассажиров была автоматически удалена, так как их маршруты вышли за допустимые пределы.';
  }
});

const canSubmit = computed(() => {
  const normalizedFloorCount = Number(floorCount.value);

  return Number.isInteger(normalizedFloorCount) && normalizedFloorCount >= 2 && normalizedFloorCount <= 50;
});

function submit(): void {
  if (!canSubmit.value) {
    return;
  }

  emit('create', {
    name: sessionName.value.trim() || null,
    floorCount: Math.trunc(Number(floorCount.value)),
    initialPassengers: [...initialPassengers.value]
  });
}

function reset(): void {
  sessionName.value = '';
  floorCount.value = 9;
  initialPassengers.value = [];
  passengerAdjustmentMessage.value = '';
}

defineExpose({
  reset
});
</script>

<template>
  <SectionCard
    title="Создание сеанса"
    subtitle="Укажите параметры нового сеанса моделирования и при необходимости задайте стартовый список пассажиров."
  >
    <v-row>
      <v-col cols="12" md="6">
        <v-text-field
          v-model="sessionName"
          label="Название сеанса"
          maxlength="120"
          counter
          placeholder="Например: Учебный сеанс №1"
        ></v-text-field>
      </v-col>

      <v-col cols="12" md="6">
        <v-text-field
          v-model.number="floorCount"
          label="Количество этажей"
          type="number"
          min="2"
          max="50"
          step="1"
        ></v-text-field>
      </v-col>
    </v-row>

    <v-alert v-if="passengerAdjustmentMessage" type="warning" variant="tonal" class="mb-4">
      {{ passengerAdjustmentMessage }}
    </v-alert>

    <InitialPassengerListEditor
      v-model="initialPassengers"
      :floor-count="Number(floorCount)"
    ></InitialPassengerListEditor>

    <div class="d-flex justify-end mt-6">
      <v-btn
        color="primary"
        variant="flat"
        prepend-icon="mdi-plus-circle"
        :loading="props.busy"
        :disabled="!canSubmit"
        @click="submit"
      >
        Создать сеанс
      </v-btn>
    </div>
  </SectionCard>
</template>
