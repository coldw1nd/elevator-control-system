import type { SessionReportDto } from '@/types/api';
import { apiClient } from '@/services/http-client';

export interface DownloadedFileResult {
  blob: Blob;
  fileName: string;
}

function tryExtractFileName(contentDisposition?: string): string | null {
  if (!contentDisposition) {
    return null;
  }

  // Backend может прислать имя файла как filename=... или filename*=UTF-8''...
  // Поддерживаем оба варианта, чтобы сохранять отчет под осмысленным именем.
  const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);

  if (utf8Match?.[1]) {
    try {
      return decodeURIComponent(utf8Match[1].trim().replace(/["']/g, ''));
    } catch {
      return utf8Match[1].trim().replace(/["']/g, '');
    }
  }

  const regularMatch = contentDisposition.match(/filename="?([^"]+)"?/i);

  if (regularMatch?.[1]) {
    return regularMatch[1].trim();
  }

  return null;
}

export const reportService = {
  async getReport(sessionId: string): Promise<SessionReportDto> {
    const { data } = await apiClient.get<SessionReportDto>(`/api/sessions/${sessionId}/report`);

    return data;
  },

  async downloadExcel(sessionId: string): Promise<DownloadedFileResult> {
    const response = await apiClient.get(`/api/sessions/${sessionId}/report/excel`, {
      responseType: 'blob'
    });

    const contentDisposition = response.headers['content-disposition'] as string | undefined;
    const fileName = tryExtractFileName(contentDisposition) ?? `report_${sessionId}.xlsx`;

    return {
      blob: response.data as Blob,
      fileName
    };
  }
};
