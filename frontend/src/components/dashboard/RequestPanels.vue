<script setup lang="ts">
import { computed } from 'vue';
import type { SessionSnapshotDto } from '@/types/api';
import SectionCard from '@/components/common/SectionCard.vue';

const props = defineProps<{
  snapshot: SessionSnapshotDto | null;
}>();

const floorNumbers = computed(() => {
  if (!props.snapshot) {
    return [];
  }

  return Array.from(
    { length: props.snapshot.floorCount },
    (_, index) => props.snapshot!.floorCount - index
  );
});

function isFloorCallPressed(floorNumber: number): boolean {
  return props.snapshot?.floorCalls.some(
    (call) => call.floorNumber === floorNumber && call.isPressed
  ) ?? false;
}

function isCabinRequestPressed(floorNumber: number): boolean {
  return props.snapshot?.cabinRequests.some(
    (request) => request.floorNumber === floorNumber && request.isPressed
  ) ?? false;
}
</script>

<template>
  <v-row>
    <v-col cols="12" lg="6">
      <SectionCard
        title="Кнопки вызова на этажах"
        subtitle="Нажатые кнопки вызова автоматически сбрасываются после остановки лифта на соответствующем этаже."
      >
        <div class="request-grid">
          <div
            v-for="floorNumber in floorNumbers"
            :key="`floor-call-${floorNumber}`"
            class="request-tile"
            :class="{
              'request-tile--active': isFloorCallPressed(floorNumber),
              'request-tile--warning': isFloorCallPressed(floorNumber)
            }"
          >
            <div class="text-body-2 font-weight-bold">Этаж {{ floorNumber }}</div>
            <div class="text-caption mt-1">
              {{ isFloorCallPressed(floorNumber) ? 'Вызов активен' : 'Нет активного вызова' }}
            </div>
          </div>
        </div>
      </SectionCard>
    </v-col>

    <v-col cols="12" lg="6">
      <SectionCard
        title="Кнопки внутри кабины"
        subtitle="Нажатая кнопка означает наличие задания на перемещение лифта к выбранному этажу."
      >
        <div class="request-grid">
          <div
            v-for="floorNumber in floorNumbers"
            :key="`cabin-request-${floorNumber}`"
            class="request-tile"
            :class="{
              'request-tile--active': isCabinRequestPressed(floorNumber),
              'request-tile--primary': isCabinRequestPressed(floorNumber)
            }"
          >
            <div class="text-body-2 font-weight-bold">Этаж {{ floorNumber }}</div>
            <div class="text-caption mt-1">
              {{ isCabinRequestPressed(floorNumber) ? 'Задание активно' : 'Задание отсутствует' }}
            </div>
          </div>
        </div>
      </SectionCard>
    </v-col>
  </v-row>
</template>

<style scoped>
.request-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
  gap: 12px;
}

.request-tile {
  padding: 14px 16px;
  border-radius: 16px;
  border: 1px solid rgba(148, 163, 184, 0.22);
  background: #f8fafc;
  transition:
    transform 180ms ease,
    box-shadow 180ms ease,
    border-color 180ms ease;
}

.request-tile--active {
  transform: translateY(-1px);
  box-shadow: 0 10px 20px rgba(15, 23, 42, 0.08);
}

.request-tile--warning {
  border-color: rgba(245, 158, 11, 0.36);
  background: rgba(245, 158, 11, 0.1);
}

.request-tile--primary {
  border-color: rgba(37, 99, 235, 0.32);
  background: rgba(37, 99, 235, 0.08);
}
</style>
