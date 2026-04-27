using ClosedXML.Excel;
using ElevatorControlSystem.Application;
using ElevatorControlSystem.Domain;

namespace ElevatorControlSystem.Infrastructure;

public sealed class ExcelReportExportService : IReportExportService
{
    public Task<byte[]> ExportExcelAsync(
        SimulationSession session,
        SessionReport report,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var workbook = new XLWorkbook();

        var summarySheet = workbook.Worksheets.Add("Summary");
        var passengersSheet = workbook.Worksheets.Add("Passengers");

        summarySheet.Cell(1, 1).Value = "Показатель";
        summarySheet.Cell(1, 2).Value = "Значение";

        summarySheet.Cell(2, 1).Value = "Название сеанса";
        summarySheet.Cell(2, 2).Value = session.Name;

        summarySheet.Cell(3, 1).Value = "Количество этажей";
        summarySheet.Cell(3, 2).Value = session.FloorCount;

        summarySheet.Cell(4, 1).Value = "Время запуска";
        summarySheet.Cell(4, 2).Value = session.StartedAtUtc?.ToString("u") ?? string.Empty;

        summarySheet.Cell(5, 1).Value = "Время остановки";
        summarySheet.Cell(5, 2).Value = session.StoppedAtUtc?.ToString("u") ?? string.Empty;

        summarySheet.Cell(6, 1).Value = "Общее количество поездок";
        summarySheet.Cell(6, 2).Value = report.TotalTrips;

        summarySheet.Cell(7, 1).Value = "Количество холостых поездок";
        summarySheet.Cell(7, 2).Value = report.EmptyTrips;

        summarySheet.Cell(8, 1).Value = "Суммарный перемещенный вес";
        summarySheet.Cell(8, 2).Value = report.TotalTransportedWeightKg;

        summarySheet.Cell(9, 1).Value = "Количество созданных объектов \"человек\"";
        summarySheet.Cell(9, 2).Value = report.TotalCreatedPassengers;

        summarySheet.Cell(10, 1).Value = "Отчет сформирован";
        summarySheet.Cell(10, 2).Value = report.GeneratedAtUtc.ToString("u");

        var summaryRange = summarySheet.Range(1, 1, 10, 2);
        summaryRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        summaryRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        summarySheet.Row(1).Style.Font.Bold = true;
        summarySheet.Columns().AdjustToContents();

        passengersSheet.Cell(1, 1).Value = "Id";
        passengersSheet.Cell(1, 2).Value = "Вес, кг";
        passengersSheet.Cell(1, 3).Value = "Этаж появления";
        passengersSheet.Cell(1, 4).Value = "Целевой этаж";
        passengersSheet.Cell(1, 5).Value = "Текущий этаж";
        passengersSheet.Cell(1, 6).Value = "Статус";
        passengersSheet.Cell(1, 7).Value = "Создан";
        passengersSheet.Cell(1, 8).Value = "Нажал вызов";
        passengersSheet.Cell(1, 9).Value = "Вошел в лифт";
        passengersSheet.Cell(1, 10).Value = "Доставлен";

        var rowIndex = 2;

        foreach (var passenger in session.Passengers.OrderBy(x => x.CreatedAtUtc))
        {
            passengersSheet.Cell(rowIndex, 1).Value = passenger.Id.ToString();
            passengersSheet.Cell(rowIndex, 2).Value = passenger.WeightKg;
            passengersSheet.Cell(rowIndex, 3).Value = passenger.SourceFloor;
            passengersSheet.Cell(rowIndex, 4).Value = passenger.TargetFloor;
            passengersSheet.Cell(rowIndex, 5).Value = passenger.CurrentFloor;
            passengersSheet.Cell(rowIndex, 6).Value = passenger.Status.ToString();
            passengersSheet.Cell(rowIndex, 7).Value = passenger.CreatedAtUtc.ToString("u");
            passengersSheet.Cell(rowIndex, 8).Value = passenger.CallPressedAtUtc?.ToString("u") ?? string.Empty;
            passengersSheet.Cell(rowIndex, 9).Value = passenger.BoardedAtUtc?.ToString("u") ?? string.Empty;
            passengersSheet.Cell(rowIndex, 10).Value = passenger.DeliveredAtUtc?.ToString("u") ?? string.Empty;
            rowIndex++;
        }

        if (rowIndex > 2)
        {
            var passengersRange = passengersSheet.Range(1, 1, rowIndex - 1, 10);
            passengersRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            passengersRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        passengersSheet.Row(1).Style.Font.Bold = true;
        passengersSheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(stream.ToArray());
    }
}
