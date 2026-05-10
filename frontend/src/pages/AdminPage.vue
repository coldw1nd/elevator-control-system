<script setup lang="ts">
import { onMounted, ref } from 'vue';
import AppShell from '@/components/common/AppShell.vue';
import EmptyState from '@/components/common/EmptyState.vue';
import GlobalSnackbar from '@/components/common/GlobalSnackbar.vue';
import LoadingPanel from '@/components/common/LoadingPanel.vue';
import SectionCard from '@/components/common/SectionCard.vue';
import UserFormDialog from '@/components/admin/UserFormDialog.vue';
import UsersTable from '@/components/admin/UsersTable.vue';
import { useOptionalStatusBar } from '@/composables/useOptionalStatusBar';
import { usePageAccess } from '@/composables/usePageAccess';
import { useSnackbar } from '@/composables/useSnackbar';
import { adminService } from '@/services/admin-service';
import { authService } from '@/services/auth-service';
import type {
  CreateUserRequestDto,
  UpdateUserRequestDto,
  UserDto
} from '@/types/api';
import { extractErrorMessage } from '@/utils/errors';

const { currentUser, loading, accessError, initialize } = usePageAccess(['Admin']);
const { statusBar, refreshStatusBar } = useOptionalStatusBar();
const { snackbar, showError, showSuccess } = useSnackbar();

const users = ref<UserDto[]>([]);
const loadingUsers = ref(true);
const busyAction = ref(false);

const dialogVisible = ref(false);
const dialogMode = ref<'create' | 'edit'>('create');
const editedUser = ref<UserDto | null>(null);

async function loadUsers(): Promise<void> {
  loadingUsers.value = true;

  try {
    users.value = await adminService.getUsers();
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    loadingUsers.value = false;
  }
}

function openCreateDialog(): void {
  dialogMode.value = 'create';
  editedUser.value = null;
  dialogVisible.value = true;
}

function openEditDialog(user: UserDto): void {
  dialogMode.value = 'edit';
  editedUser.value = user;
  dialogVisible.value = true;
}

async function saveUser(request: CreateUserRequestDto | UpdateUserRequestDto): Promise<void> {
  busyAction.value = true;

  try {
    if (dialogMode.value === 'create') {
      await adminService.createUser(request as CreateUserRequestDto);
      showSuccess('Пользователь успешно создан.');
    } else if (editedUser.value) {
      await adminService.updateUser(editedUser.value.id, request as UpdateUserRequestDto);
      showSuccess('Пользователь успешно обновлен.');
    }

    dialogVisible.value = false;
    editedUser.value = null;

    await loadUsers();
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
  }
}

async function toggleUserActivity(user: UserDto): Promise<void> {
  busyAction.value = true;

  try {
    await adminService.toggleUserActivity(user.id);
    await loadUsers();

    showSuccess('Состояние учетной записи изменено.');
  } catch (error) {
    showError(extractErrorMessage(error));
  } finally {
    busyAction.value = false;
  }
}

async function handleLogout(): Promise<void> {
  await authService.logout();
  window.location.href = '/';
}

onMounted(async () => {
  await initialize();

  if (accessError.value) {
    loadingUsers.value = false;
    return;
  }

  await Promise.all([
    loadUsers(),
    refreshStatusBar()
  ]);
});
</script>

<template>
  <AppShell
    page-title="Администрирование пользователей"
    active-page="admin"
    :current-user="currentUser"
    :status-bar="statusBar"
    @logout="handleLogout"
  >
    <template #toolbar>
      <v-btn
        v-if="!loading && !accessError"
        color="primary"
        variant="flat"
        prepend-icon="mdi-account-plus"
        @click="openCreateDialog"
      >
        Новый пользователь
      </v-btn>
    </template>

    <template v-if="loading || loadingUsers">
      <LoadingPanel text="Загрузка списка пользователей..." />
    </template>

    <template v-else-if="accessError">
      <EmptyState
        title="Недостаточно прав"
        description="Данный раздел доступен только администраторам."
        icon="mdi-shield-alert-outline"
        action-text="К мониторингу"
        action-href="/dashboard/"
      />
    </template>

    <template v-else>
      <SectionCard
        title="Пользователи системы"
        subtitle="В данном разделе администратор может создавать пользователей, менять их роли и управлять активностью учетных записей."
      >
        <UsersTable
          :users="users"
          :current-user-id="currentUser?.id"
          @edit="openEditDialog"
          @toggle-activity="toggleUserActivity"
        />
      </SectionCard>
    </template>

    <UserFormDialog
      v-model="dialogVisible"
      :mode="dialogMode"
      :user="editedUser"
      :busy="busyAction"
      @save="saveUser"
    />

    <GlobalSnackbar
      v-model="snackbar.visible"
      :text="snackbar.text"
      :color="snackbar.color"
      :timeout="snackbar.timeout"
    />
  </AppShell>
</template>
