<script setup lang="ts">
withDefaults(
  defineProps<{
    text?: string;
  }>(),
  {
    text: 'Загрузка данных...'
  }
);
</script>

<template>
  <v-card class="pa-10 text-center">
    <v-progress-circular indeterminate color="primary" size="64"></v-progress-circular>
    <div class="text-body-1 mt-4">{{ text }}</div>
  </v-card>
</template>
