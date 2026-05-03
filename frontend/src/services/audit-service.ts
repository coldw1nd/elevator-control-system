import type { AuditLogDto, AuditLogQueryDto } from '@/types/api';
import { apiClient } from '@/services/http-client';

export const auditService = {
  async getAuditLogs(query: AuditLogQueryDto): Promise<AuditLogDto[]> {
    const { data } = await apiClient.get<AuditLogDto[]>('/api/audit', {
      params: query
    });

    return data;
  }
};
