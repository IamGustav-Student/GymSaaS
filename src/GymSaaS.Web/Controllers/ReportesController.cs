using System.Text;
using GymSaaS.Application.Reportes.Queries.GetReporteIngresos;
using GymSaaS.Application.Reportes.Queries.GetReporteOcupacionClases;
using GymSaaS.Application.Reportes.Queries.GetReporteSocios;
using GymSaaS.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSaaS.Web.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class ReportesController : Controller
    {
        private readonly IMediator _mediator;
        private const int MesesPorDefecto = 6;

        public ReportesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        public class ReportesViewModel
        {
            public List<IngresoMensualDto> Ingresos { get; set; } = new();
            public List<SociosMensualDto> Socios { get; set; } = new();
            public ReporteOcupacionDto Ocupacion { get; set; } = new();
        }

        public async Task<IActionResult> Index()
        {
            var vm = new ReportesViewModel
            {
                Ingresos = await _mediator.Send(new GetReporteIngresosQuery(MesesPorDefecto)),
                Socios = await _mediator.Send(new GetReporteSociosQuery(MesesPorDefecto)),
                Ocupacion = await _mediator.Send(new GetReporteOcupacionClasesQuery())
            };

            return View(vm);
        }

        public async Task<IActionResult> ExportarIngresosCsv()
        {
            var ingresos = await _mediator.Send(new GetReporteIngresosQuery(MesesPorDefecto));

            var csv = new StringBuilder();
            csv.AppendLine("Mes,Total");
            foreach (var i in ingresos)
            {
                csv.AppendLine($"{i.MesLabel},{i.Total.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(bytes, "text/csv", "ingresos.csv");
        }
    }
}
