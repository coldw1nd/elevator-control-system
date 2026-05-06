<script setup lang="ts">
import { computed } from 'vue';
import type { SessionSnapshotDto } from '@/types/api';
import { formatRoundedPosition } from '@/utils/format';

const props = defineProps<{
  snapshot: SessionSnapshotDto | null;
}>();

const floorHeight = 104;
const carHeight = 90;
const shaftTopPadding = 20;
const shaftBottomPadding = 5;
const carVerticalOffset = (floorHeight - carHeight) / 2;

const floors = computed(() => {
  if (!props.snapshot) {
    return [];
  }

  return Array.from(
    { length: props.snapshot.floorCount },
    (_, index) => props.snapshot!.floorCount - index
  );
});

const shaftHeight = computed(() => {
  if (!props.snapshot) {
    return shaftTopPadding + (floorHeight * 6) + shaftBottomPadding;
  }

  return shaftTopPadding + (props.snapshot.floorCount * floorHeight) + shaftBottomPadding;
});

const elevatorTop = computed(() => {
  if (!props.snapshot) {
    return shaftTopPadding;
  }

  return shaftTopPadding
    + ((props.snapshot.floorCount - props.snapshot.elevator.currentPosition) * floorHeight)
    + carVerticalOffset;
});

function hasFloorCall(floorNumber: number): boolean {
  return props.snapshot?.floorCalls.some(
    (call) => call.floorNumber === floorNumber && call.isPressed
  ) ?? false;
}

function hasCabinRequest(floorNumber: number): boolean {
  return props.snapshot?.cabinRequests.some(
    (request) => request.floorNumber === floorNumber && request.isPressed
  ) ?? false;
}
</script>

<template>
  <div class="elevator-board">
    <div
      class="elevator-board__shaft"
      :style="{
        height: `${shaftHeight}px`,
        '--shaft-top-padding': `${shaftTopPadding}px`,
        '--shaft-bottom-padding': `${shaftBottomPadding}px`
      }"
    >
      <div
        v-for="floorNumber in floors"
        :key="floorNumber"
        class="elevator-board__floor"
        :style="{ height: `${floorHeight}px` }"
      >
        <div class="elevator-board__floor-label">
          <div class="text-body-2 font-weight-bold">Этаж {{ floorNumber }}</div>

          <div class="d-flex ga-2 flex-wrap mt-1">
            <v-chip
              size="x-small"
              :color="hasFloorCall(floorNumber) ? 'warning' : 'secondary'"
              variant="tonal"
            >
              Вызов
            </v-chip>

            <v-chip
              size="x-small"
              :color="hasCabinRequest(floorNumber) ? 'primary' : 'secondary'"
              variant="tonal"
            >
              Кабина
            </v-chip>
          </div>
        </div>
      </div>

      <div
        v-if="snapshot"
        class="elevator-board__car"
        :class="{
          'elevator-board__car--open': snapshot.elevator.doorsAreOpen,
          'elevator-board__car--overload': snapshot.elevator.overloadIndicatorOn
        }"
        :style="{ transform: `translateY(${elevatorTop}px)` }"
      >
        <div class="elevator-board__car-display">
          {{ formatRoundedPosition(snapshot.elevator.currentPosition) }}
        </div>

        <div class="elevator-board__car-inner">
          <div class="elevator-board__door elevator-board__door--left"></div>
          <div class="elevator-board__door elevator-board__door--right"></div>
        </div>

        <div class="elevator-board__car-caption">
          {{ snapshot.elevator.passengerCount }} чел.
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.elevator-board {
  overflow-x: auto;
}

.elevator-board__shaft {
  position: relative;
  min-width: 348px;
  border-radius: 24px;
  background:
    linear-gradient(180deg, rgba(248, 250, 252, 1) 0%, rgba(226, 232, 240, 1) 100%);
  border: 1px solid rgba(148, 163, 184, 0.25);
  padding: var(--shaft-top-padding) 18px var(--shaft-bottom-padding) 116px;
  overflow: hidden;
}

.elevator-board__floor {
  position: relative;
  display: flex;
  align-items: center;
  border-top: 1px dashed rgba(100, 116, 139, 0.35);
}

.elevator-board__floor:first-child {
  border-top: none;
}

.elevator-board__floor-label {
  margin-left: 108px;
  padding-bottom: 2px;
}

.elevator-board__car {
  position: absolute;
  top: 0;
  left: 24px;
  width: 70px;
  height: 90px;
  border-radius: 22px;
  border: 1px solid rgba(255, 255, 255, 0.18);
  background:
    linear-gradient(180deg, rgba(15, 23, 42, 0.98) 0%, rgba(30, 41, 59, 0.98) 100%);
  box-shadow: 0 20px 42px rgba(15, 23, 42, 0.24);
  transition:
    transform 500ms linear,
    box-shadow 300ms ease,
    background 300ms ease;
  z-index: 3;
}

.elevator-board__car--overload {
  box-shadow: 0 20px 42px rgba(220, 38, 38, 0.28);
  background:
    linear-gradient(180deg, rgba(127, 29, 29, 0.98) 0%, rgba(153, 27, 27, 0.98) 100%);
}

.elevator-board__car-display {
  position: absolute;
  top: -14px;
  right: -12px;
  min-width: 46px;
  padding: 4px 8px;
  border-radius: 999px;
  background: #ffffff;
  border: 1px solid rgba(148, 163, 184, 0.28);
  box-shadow: 0 10px 20px rgba(15, 23, 42, 0.12);
  font-size: 12px;
  font-weight: 700;
  text-align: center;
}

.elevator-board__car-inner {
  position: absolute;
  inset: 10px 10px 20px 10px;
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2px;
  overflow: hidden;
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.06);
}

.elevator-board__door {
  transition: transform 320ms ease;
  background:
    linear-gradient(180deg, rgba(147, 197, 253, 1) 0%, rgba(96, 165, 250, 1) 100%);
}

.elevator-board__door--right {
  background:
    linear-gradient(180deg, rgba(125, 211, 252, 1) 0%, rgba(56, 189, 248, 1) 100%);
}

.elevator-board__car--open .elevator-board__door--left {
  transform: translateX(-16px);
}

.elevator-board__car--open .elevator-board__door--right {
  transform: translateX(16px);
}

.elevator-board__car-caption {
  position: absolute;
  left: 0;
  right: 0;
  bottom: 5px;
  text-align: center;
  font-size: 11px;
  line-height: 1;
  color: #e2e8f0;
  font-weight: 600;
}
</style>
