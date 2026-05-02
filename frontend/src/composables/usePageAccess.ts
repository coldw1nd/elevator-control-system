import { ref } from 'vue';
import type { CurrentUserDto, UserRole } from '@/types/api';
import { authService } from '@/services/auth-service';
import { getStoredUser } from '@/services/auth-storage';
import { redirectToLogin } from '@/utils/urls';

export function usePageAccess(requiredRoles: UserRole[] = []) {
  const currentUser = ref<CurrentUserDto | null>(getStoredUser());
  const loading = ref(true);
  const accessError = ref('');

  async function initialize(): Promise<void> {
    loading.value = true;
    accessError.value = '';

    if (!authService.isAuthenticated()) {
      redirectToLogin();
      return;
    }

    try {
      const user = await authService.getCurrentUser();

      currentUser.value = user;

      if (requiredRoles.length > 0 && !requiredRoles.includes(user.role)) {
        accessError.value =
          'У текущего пользователя недостаточно прав для просмотра данной страницы.';
      }
    } catch {
      redirectToLogin();
      return;
    } finally {
      loading.value = false;
    }
  }

  return {
    currentUser,
    loading,
    accessError,
    initialize
  };
}
