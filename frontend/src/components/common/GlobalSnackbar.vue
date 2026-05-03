<script setup lang="ts">
import type { SnackbarColor } from '@/types/api';

const visible = defineModel<boolean>({
  required: true
});

defineProps<{
  text: string;
  color: SnackbarColor;
  timeout?: number;
}>();
</script>

<template>
  <v-snackbar
    v-model="visible"
    :timeout="timeout ?? 4000"
    :color="color"
    location="bottom right"
  >
    {{ text }}
  </v-snackbar>
</template>
