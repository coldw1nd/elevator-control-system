<script setup lang="ts">
const visible = defineModel<boolean>({
  required: true
});

const props = withDefaults(
  defineProps<{
    title: string;
    text: string;
    confirmText?: string;
    cancelText?: string;
    color?: string;
  }>(),
  {
    confirmText: 'Подтвердить',
    cancelText: 'Отмена',
    color: 'error'
  }
);

const emit = defineEmits<{
  confirm: [];
}>();

function handleConfirm(): void {
  emit('confirm');
  visible.value = false;
}
</script>

<template>
  <v-dialog v-model="visible" max-width="520">
    <v-card>
      <v-card-title class="text-h6">{{ props.title }}</v-card-title>

      <v-card-text class="text-body-1">
        {{ props.text }}
      </v-card-text>

      <v-card-actions class="justify-end">
        <v-btn variant="text" @click="visible = false">{{ props.cancelText }}</v-btn>
        <v-btn :color="props.color" variant="flat" @click="handleConfirm">
          {{ props.confirmText }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
