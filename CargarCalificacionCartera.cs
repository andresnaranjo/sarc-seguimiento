using Opa.Auditoria.Entidades.PartialClass;
using Opa.Auditoria.Negocio.Fachada;
using Opa.Framework.Integration.Jobs.PartialClass;
using Opa.Framework.Integration.Negocio.Fachada;
using Opa.Herramientas.Librerias.Exceptions;
using Opa.Herramientas.Librerias.Negocio.Entidades;
using Opa.Herramientas.Librerias.Servicios.Fachadas;
using Opa.Sarc.Entidades;
using Opa.Sarc.Entidades.ClasesParciales;
using Opa.Sarc.Entidades.PartialClass;
using Opa.Sarc.Negocio.Fachada;
using Opa.Transaccional.Entidades;
using Opa.Transaccional.Negocio.Fachada;
using Quartz;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Opa.Framework.Integration.Jobs.Tasks
{
    public class CargarCalificacionCartera : IJob
    {
        public Status StatusTask { get; set; }
        public DatosRed red = new DatosRed();


        public void Execute(IJobExecutionContext context)
        {
            red = new DatosRedFacade().ObtenerDatosDeRed();

            if (this.StatusTask == Status.Running)
            {
                Task.Factory.StartNew(() => CargarScoreSeguimientoAsync());
            }
        }






        #region CargarScoreSeguimientoAsync

        public async void CargarScoreSeguimientoAsync()
        {
            OpaServicioFachada Fa = new OpaServicioFachada();
            this.StatusTask = Status.Running;
            short ValorEnum = 0;
            string nombre = "ResultadosScore";




            try
            {


                Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");
                // Guardo el log funcional de ejecucion de este proceso    
                red = new DatosRedFacade().ObtenerDatosDeRed();
                new OpaAuditoriaFachada().GuardarLogFuncionalWFAsync("Servicio windows para carga de informacion <ResultadosScore>",
                                                                     "Not Gui required",
                                                                     "<root><NoData>Not data is required to execution</NoData></root>",
                                                                     new DatosUsuarioAuditoria
                                                                     {
                                                                         NombreUsuario = "User is not required",
                                                                         CodigoUsuario = "",
                                                                         DireccionIp = red.Ip,

                                                                     });


                // 0: otorgamiento masivo  1: seguimiento
                string Estado = ConfigurationManager.AppSettings["Estado"];

                if (Estado == "0")
                {

                    OpaServicioFachada FachadaServicio = new OpaServicioFachada();
                    OpaSarcFachada bd = new OpaSarcFachada();


                    List<DatosGeneralPersona> EntidadCreditosPersona = bd.BuscarListaDatosGeneralesPersona().ToList();


                    List<Opa.Sarc.Entidades.ResultadosScore> EntidadResultado = new List<ResultadosScore>();



                    ////  el proceso normal es que se envie false solo se envia true para el efecto de que se cree Otorgamiento para todos los productos
                    bool CargarHistoria = true;
                    //bool CargarHistoria = true;      

                    foreach (DatosGeneralPersona Registro in EntidadCreditosPersona)
                    {

                        decimal CedulaPersona = Convert.ToDecimal(Registro.IdentificacionPersona);

                        string Numerosolicitud = null;

                        try
                        {
                            ArrayList ArrayCentral = new ArrayList();
                            //consultacentrales 1: SI 2:NO
                            //List<ModeloCalificacion> EntidadModeloCalificacion = new Opa.Sarc.Negocio.FlujoTrabajo.ModeloCalificacionWF().ModeloCalificacionAsociadoServicio(CedulaPersona, Numerosolicitud, 0, 0, CargarHistoria);
                            List<ModeloCalificacion> EntidadModeloCalificacion = new Opa.Sarc.Negocio.FlujoTrabajo.ModeloCalificacionWF().ModeloCalificacionAsociado(CedulaPersona, Numerosolicitud, 1, ArrayCentral, CargarHistoria, false, false);

                            if (EntidadModeloCalificacion != null && EntidadModeloCalificacion.Count > 0)
                            {
                                CapacidadPago(CedulaPersona, Numerosolicitud, EntidadModeloCalificacion.FirstOrDefault(), EntidadModeloCalificacion.FirstOrDefault().EgresosExternos);
                            }

                        }
                        catch (Exception ex)
                        {
                            string exe = ex.Message.ToString();

                        }
                    }



                }

                //cuando es seguimiento
                else
                {
                    if (Estado == "1")
                    {
                        OpaServicioFachada FachadaServicio = new OpaServicioFachada();

                        List<Sarc_ViewCreditosParaSeguimiento> EntidadCreditosPersona = new List<Sarc_ViewCreditosParaSeguimiento>();

                        EntidadCreditosPersona = new OpaSarcFachada().BuscarListaCreditosParaSeguimiento();

                        //List<ResultadosScore> EntidadCreditosPersona = new List<ResultadosScore>();

                        //List<Opa.Sarc.Entidades.ResultadosScore> EntidadResultado = new Opa.Sarc.Negocio.Logica.ResultadosScoreBL().BuscarListarResultadosScore();




                        //EntidadCreditosPersona = (from c in EntidadResultado
                        //                          where c.ScoreInicial = true
                        //                          select c).ToList();


                        bool CargarHistoria = false;
                        //bool CargarHistoria = true;  
                        OpaTransaccionalFachada FachadaTransaccional = new OpaTransaccionalFachada();
                        List<ResultadosScore> List = new List<ResultadosScore>();
                        //new Opa.Sarc.Negocio.FlujoTrabajo.ModeloCalificacionWF2().CargarDatos();
                        foreach (Sarc_ViewCreditosParaSeguimiento Registro in EntidadCreditosPersona)
                        {
                            string NroSolicitud = Registro.NroSolicitudOtorgamiento == 0 ? "" : Registro.NroSolicitudOtorgamiento.ToString();
                            decimal CedulaPersona = Convert.ToDecimal(Registro.IdentificacionPersona);
                            try
                            {
                                ArrayList ArrayCentral = new ArrayList();
                                List<ModeloCalificacion> EntidadModeloCalificacion = new Opa.Sarc.Negocio.FlujoTrabajo.ModeloCalificacionWF().ModeloCalificacionAsociado(CedulaPersona, NroSolicitud, 1, ArrayCentral, CargarHistoria, false, false);

                                if (EntidadModeloCalificacion != null && EntidadModeloCalificacion.Count > 0)
                                {
                                    CapacidadPago(CedulaPersona, EntidadModeloCalificacion.FirstOrDefault().NroSolicitud, EntidadModeloCalificacion.FirstOrDefault(), EntidadModeloCalificacion.FirstOrDefault().EgresosExternos);
                                }

                                decimal NroSol = Convert.ToDecimal(EntidadModeloCalificacion.FirstOrDefault().NroSolicitud);
                                bool existe = new OpaTransaccionalFachada().BuscarLiquidaCreditosPersonaPorSolicitud(NroSol);
                                if (!existe)
                                {
                                    CreditosPersona CreditoPersona = new OpaSarcFachada().BuscarUnCreditosPersonaPorPagare(Convert.ToInt32(Registro.NroPagare));
                                    string numeroPagare = CreditoPersona.NumeroPagare.ToString();
                                    decimal nroSolicitud = FachadaTransaccional.BuscarLiquidaCreditosPagare(numeroPagare);

                                    FachadaTransaccional.GuardarLiquidaCreditosPersona(new LiquidacreditosPersonas()
                                    {
                                        CedulaPersona = Convert.ToInt64(CedulaPersona),
                                        NumeroSolicitud = nroSolicitud,
                                        Plazo = CreditoPersona.Plazo.ToString(),
                                        coddestino = CreditoPersona.Coddestino,
                                        NumeroSolicitudOtorgamiento = Convert.ToDecimal(EntidadModeloCalificacion.FirstOrDefault().NroSolicitud),
                                        Valor = CreditoPersona.CapitalInicial,
                                        Fecha = CreditoPersona.FechaCargoPrestamo,
                                        Codlinea = CreditoPersona.Codlinea,
                                        Obligatorio = false,
                                        Reestructuracion = false,
                                        CreditosReestructurados = "",
                                        ExigeCodeudor = false,
                                        Codeudores = "",
                                        IncluyeExtras = false,
                                        CuotasExtras = "",
                                        IncluyeCostos = false,
                                        Tasa = CreditoPersona.TasaColocacion,
                                        CostosAdicionales = "",
                                        EsOtorgamientoMasivo = true

                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                string exe = ex.Message.ToString();

                            }
                        }

                        //List<ResultadosScore> Lista = new Opa.Sarc.Negocio.FlujoTrabajo.ModeloCalificacionWF2().ListarTodosResultadosScore();
                        //List<ResultadoScorePartial> ListaAux = Lista.Select((x, index) => new ResultadoScorePartial
                        //{
                        //    IdResultadosScore = x.IdResultadosScore,
                        //    CedulaPersona = (long)x.CedulaPersona,
                        //    NombrePersona = x.NombrePersona,
                        //    NroSolicitud = x.NroSolicitud,
                        //    FechaScore = (DateTime)x.FechaScore,
                        //    PesoCuantitativo = (short)x.PesoCuantitativo,
                        //    PesoCualitativo = (short)x.PesoCualitativo,
                        //    Calificacion = x.Calificacion,
                        //    ResultadosVariables = x.ResultadosVariables,
                        //    ResultadosCapacidad = x.ResultadosCapacidad,
                        //    Limites = x.Limites,
                        //    ScoreInicial = x.ScoreInicial,
                        //    IdConsultasCentral = (Guid)x.IdConsultasCentral,
                        //    IdSegmento = (Guid)x.IdSegmento,
                        //    IdentificadorOrdenador = index + 1
                        //}).ToList();
                        //InsertarRegistrosBulkInsert(ListaAux, List);
                    }
                }
            }
            catch (Exception ex)
            {
                DatosUsuarioAuditoria d = new DatosUsuarioAuditoria()
                {
                    NombreUsuario = "User is not required",
                    CodigoUsuario = "",
                    DireccionIp = red.Ip,
                };

                // Loggin error 
                Guid idlog = OpaExceptionHandling.Handle(ex, d);
                this.StatusTask = Status.Error;
                ValorEnum = (short)StatusTask;
                Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");
                return;
            }


            this.StatusTask = Status.Ok;
            ValorEnum = (short)StatusTask;
            Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");
            Task.Factory.StartNew(() => CalificacionCartera());
        }

        public void InsertarRegistrosBulkInsert(List<ResultadoScorePartial> ListaResultadosScore, List<ResultadosScore> ListaScoreCapacidades)
        {
            int registros = int.Parse(ConfigurationManager.AppSettings["Lote"]);
            List<ResultadosScore> q = new List<ResultadosScore>();

            for (int i = 0; i < (int)(Math.Ceiling((double)(ListaResultadosScore.Count()) / registros)); i++)
            {
                var query = ListaResultadosScore.Select(e => e).Where(w => (w.IdentificadorOrdenador > (i * registros)) && (w.IdentificadorOrdenador <= ((i + 1) * registros)));
                //lista = query.ToList<ResultadosScore>();
                List<ResultadoScorePartial> l = query.ToList();
                q = TransformarResultadoScore(l, ListaScoreCapacidades);

                using (SqlConnection c = new SqlConnection(ConfigurationManager.ConnectionStrings["OpaIntegrationAccess"].ConnectionString))
                {
                    c.Open();
                    var inserter = new Opa.Herramientas.Librerias.AccesoRecursos.Dao.BulkInserter<ResultadosScore>(c, "Sarc.ResultadosScore", registros);
                    inserter.Insert(q);
                    c.Close();
                }
            }
        }

        public List<ResultadosScore> TransformarResultadoScore(List<ResultadoScorePartial> lista, List<ResultadosScore> ListaScoreCapacidades)
        {
            List<ResultadosScore> RestultadoScore = new List<ResultadosScore>();
            RestultadoScore = (from c in lista
                               join d in ListaScoreCapacidades on c.IdResultadosScore equals d.IdResultadosScore
                               select new ResultadosScore
                               {
                                   IdResultadosScore = c.IdResultadosScore,
                                   CedulaPersona = c.CedulaPersona,
                                   NombrePersona = c.NombrePersona,
                                   NroSolicitud = c.NroSolicitud,
                                   FechaScore = c.FechaScore,
                                   PesoCuantitativo = c.PesoCuantitativo,
                                   PesoCualitativo = c.PesoCualitativo,
                                   Calificacion = c.Calificacion,
                                   ResultadosVariables = c.ResultadosVariables,
                                   ResultadosCapacidad = d.ResultadosCapacidad,
                                   Limites = d.Limites,
                                   ScoreInicial = c.ScoreInicial,
                                   IdConsultasCentral = c.IdConsultasCentral,
                                   IdSegmento = c.IdSegmento,

                               }).ToList();
            return RestultadoScore;
        }


        //public List<ProductosEvaluacionCapacidadMonto> CapacidadPago(decimal cedula, string nrosolicitud, ModeloCalificacion ListaModeloCalificacion, decimal EgresoExterno)
        //{
        //    OpaSarcFachada Fachada = new OpaSarcFachada();

        //    //CapacidadDePagoWF Capacidad = new CapacidadDePagoWF();
        //    ModeloCalificacion EntidadModelocalificacion = ListaModeloCalificacion;
        //    List<ProductosEvaluacionCapacidadMonto> ListaCapacidadPago = Fachada.CapacidadDePago(cedula, nrosolicitud, EntidadModelocalificacion, EgresoExterno).ToList();

        //    //List<ProductosEvaluacionCapacidadMonto> ListaCapacidadPago = new Opa.Sarc.Negocio.FlujoTrabajo.CapacidadDePagoWF2().HallarCapacidadDePago(cedula, nrosolicitud, EntidadModelocalificacion, EgresoExterno).ToList();

        //    return ListaCapacidadPago;
        //}

        public void CapacidadPago(decimal cedula, string nrosolicitud, ModeloCalificacion ListaModeloCalificacion, decimal EgresoExterno)
        {
            OpaSarcFachada Fachada = new OpaSarcFachada();

            //CapacidadDePagoWF Capacidad = new CapacidadDePagoWF();
            ModeloCalificacion EntidadModelocalificacion = ListaModeloCalificacion;
            List<ProductosEvaluacionCapacidadMonto> ListaCapacidadPago = Fachada.CapacidadDePago(cedula, nrosolicitud, EntidadModelocalificacion, EgresoExterno).ToList();
        }

        #endregion

        #region PerdidaEsperada

        public async void CalificacionCartera()
        {
            OpaServicioFachada Fa = new OpaServicioFachada();
            this.StatusTask = Status.Running;
            short ValorEnum = 0;
            string nombre = "CalificacionCartera";
            try
            {
                Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");

                // Guardo el log funcional de ejecucion de este proceso    
                red = new DatosRedFacade().ObtenerDatosDeRed();
                new OpaAuditoriaFachada().GuardarLogFuncionalWFAsync("Servicio windows para carga de informacion <CalificacionCartera>",
                                                                     "Not Gui required",
                                                                     "<root><NoData>Not data is required to execution</NoData></root>",
                                                                     new DatosUsuarioAuditoria
                                                                     {
                                                                         NombreUsuario = "User is not required",
                                                                         CodigoUsuario = "",
                                                                         DireccionIp = red.Ip,

                                                                     });



                List<SarcCreditosLiquidados_Result> CreditosLiquidados = new Opa.Transaccional.Negocio.Fachada.OpaTransaccionalFachada().BuscarCreditosLiquidados();


                OpaSarcFachada Fc = new OpaSarcFachada();
                List<SeguimientoCalificacionCartera> ListaSegumiento = new List<SeguimientoCalificacionCartera>();
                SeguimientoCalificacionCartera DatosSEguimientoAsociado = new SeguimientoCalificacionCartera();
                string calificacion = string.Empty;

                if (CreditosLiquidados.Count() > 0)
                {
                    int NroComite = 0;
                    ConsecutivosComiteCartera entidadconsecutivo = Fc.buscarconsecutivoComite();
                    if (entidadconsecutivo != null)
                    {
                        NroComite = Convert.ToInt32(entidadconsecutivo.NroComite) + 1;
                        ConsecutivosComiteCartera entidadconsecutivoactualizar = new ConsecutivosComiteCartera
                        {
                            IdConsecutivosComiteCartera = entidadconsecutivo.IdConsecutivosComiteCartera,
                            NroComite = NroComite
                        };
                        Fc.actualizarconsecutivoComite(entidadconsecutivoactualizar);
                    }
                    else
                    {
                        NroComite += 1;
                        Fc.guardarconsecutivoComite();
                    }

                    foreach (SarcCreditosLiquidados_Result item in CreditosLiquidados)
                    {
                        List<ResultadosScore> Scores = Fc.ListaTop2ResultadoScoreAsociado(item.IdentificacionPersona);
                        if (Scores.Count == 2)
                            DatosSEguimientoAsociado = new Opa.Sarc.Negocio.FlujoTrabajo.GenerarSeguimientoWF().DevolverCalificacionPropuesta(Scores[1], Scores[0], item.IdentificacionPersona.ToString(), NroComite);
                        else
                            DatosSEguimientoAsociado = new Opa.Sarc.Negocio.FlujoTrabajo.GenerarSeguimientoWF().DevolverCalificacionPropuesta(null, Scores[0], item.IdentificacionPersona.ToString(), NroComite);

                        ListaSegumiento.Add(DatosSEguimientoAsociado);
                    }

                    XElement ModeloCalificacionXml = new XElement("root");
                    foreach (SeguimientoCalificacionCartera Fila in ListaSegumiento)
                    {
                        ModeloCalificacionXml.Add(
                                                               new XElement("SeguimientoCartera",
                                                               new XElement("IdSeguimientoCalificacionCartera", Fila.IdSeguimientoCalificacionCartera),
                                                               new XElement("FechaSeguimiento", Fila.FechaSeguimiento),
                                                               new XElement("IdentificacionPersona", Fila.IdentificacionPersona),
                                                               new XElement("NroComite", Fila.NroComite),
                                                               new XElement("CalificacionScoringInicial", Fila.CalificacionScoringInicial),
                                                               new XElement("CalificacionScoringFinal", Fila.CalificacionScoringFinal),
                                                               new XElement("CuberturaGarantia", Fila.CuberturaGarantia),
                                                             new XElement("SolvenciaInicial", Fila.SolvenciaInicial),
                                                             new XElement("SolvenciaFinal", Fila.SolvenciaFinal),
                                                             new XElement("PorcentajeSolvencia", Fila.PorcentajeSolvencia),
                                                             new XElement("CapacidadPagoInicial", Fila.CapacidadPagoInicial),
                                                             new XElement("CapacidadPagoFinal", Fila.CapacidadPagoFinal),
                                                             new XElement("PorcentajeCapacidadPago", Fila.PorcentajeCapacidadPago),
                                                             new XElement("NumeroReestructuraciones", Fila.NumeroReestructuraciones),
                                                             new XElement("NumeroNovaciones", Fila.NumeroNovaciones),
                                                             new XElement("SaldoTotalDeuda", Fila.SaldoTotalDeuda),
                                                             new XElement("CalificacionActual", Fila.CalificacionActual),
                            //new XElement("ProvisionActual", Fila.ProvisionActual),
                                                             new XElement("CalificacionPropuesta", Fila.CalificacionPropuesta),
                            //new XElement("ProvisionPropuesta", Fila.ProvisionPropuesta),
                                                             new XElement("CalificacionDefinitiva", Fila.CalificacionDefinitiva),
                            //new XElement("ProvisionDefinitiva", Fila.ProvisionDefinitiva),
                                                             new XElement("FechaCalificacionDefinitiva", Fila.FechaCalificacionDefinitiva),
                                                             new XElement("Observaciones", Fila.Observaciones)
                                                  ));
                    };

                    string xmlEnviar = ModeloCalificacionXml.ToString();

                    Fc.InsertarRegistrosSeguimientoCalificacionCartera(xmlEnviar);
                }


            }
            catch (Exception ex)
            {
                DatosUsuarioAuditoria d = new DatosUsuarioAuditoria()
                {
                    NombreUsuario = "User is not required",
                    CodigoUsuario = "",
                    DireccionIp = red.Ip,
                };

                // Loggin error 
                Guid idlog = OpaExceptionHandling.Handle(ex, d);
                this.StatusTask = Status.Error;
                ValorEnum = (short)StatusTask;
                Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");
                return;
            }


            this.StatusTask = Status.Ok;
            ValorEnum = (short)StatusTask;
            Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");
        }


        //public async void CargarPerdidaEsperadaAsync()
        //{
        //    OpaServicioFachada Fa = new OpaServicioFachada();
        //    this.StatusTask = Status.Running;
        //    short ValorEnum = 0;
        //    string nombre = "PerdidaEsperada";
        //    try
        //    {
        //        Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");

        //        // Guardo el log funcional de ejecucion de este proceso    
        //        red = new DatosRedFacade().ObtenerDatosDeRed();
        //        new OpaAuditoriaFachada().GuardarLogFuncionalWFAsync("Servicio windows para carga de informacion <PerdidaEsperada>",
        //                                                             "Not Gui required",
        //                                                             "<root><NoData>Not data is required to execution</NoData></root>",
        //                                                             new DatosUsuarioAuditoria
        //                                                             {
        //                                                                 NombreUsuario = "User is not required",
        //                                                                 CodigoUsuario = "",
        //                                                                 DireccionIp = red.Ip,

        //                                                             });


        //        OpaServicioFachada FachadaServicio = new OpaServicioFachada();

        //        List<Opa.Sarc.Entidades.ResultadosScore> EntidadResultado = new Opa.Sarc.Negocio.Logica.ResultadosScoreBL().BuscarListarResultadosScore();

        //        List<CreditosPersona> EntidadCreditosPersona = new Opa.Sarc.Negocio.Logica.CreditosPersonaBL().BuscarListaCreditosPersona();

        //        EntidadCreditosPersona = (from c in EntidadCreditosPersona
        //                                  join d in EntidadResultado on c.IdentificacionPersona equals d.CedulaPersona
        //                                  where d.ScoreInicial = true
        //                                  select c).ToList();



        //        XDocument xdoc = new XDocument(new XElement("Credito",
        //        from pag in EntidadCreditosPersona
        //        select new XElement("NumeroPagare", pag.NumeroPagare)
        //                                      ));

        //        Opa.Sarc.Negocio.Fachada.OpaSarcFachada fachadasarc = new Opa.Sarc.Negocio.Fachada.OpaSarcFachada();
        //        List<PerdidaEsperadaPersona> entidadperdidaesperado = fachadasarc.DevolverPerdidaEsperadaPorXml(xdoc);

        //    }
        //    catch (Exception ex)
        //    {
        //        DatosUsuarioAuditoria d = new DatosUsuarioAuditoria()
        //        {
        //            NombreUsuario = "User is not required",
        //            CodigoUsuario = "",
        //            DireccionIp = red.Ip,
        //        };

        //        // Loggin error 
        //        Guid idlog = OpaExceptionHandling.Handle(ex, d);
        //        this.StatusTask = Status.Error;
        //        ValorEnum = (short)StatusTask;
        //        Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");
        //        return;
        //    }


        //    this.StatusTask = Status.Ok;
        //    ValorEnum = (short)StatusTask;
        //    Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");
        //}





        #endregion


        #region nuevoSeguimiento

        public async void CargarScoreSeguimientoAsyncNuevoSeguimiento()
        {
            OpaServicioFachada Fa = new OpaServicioFachada();
            this.StatusTask = Status.Running;
            short ValorEnum = 0;
            string nombre = "ResultadosScore";




            try
            {


                Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");
                // Guardo el log funcional de ejecucion de este proceso    
                red = new DatosRedFacade().ObtenerDatosDeRed();
                new OpaAuditoriaFachada().GuardarLogFuncionalWFAsync("Servicio windows para carga de informacion <ResultadosScore>",
                                                                     "Not Gui required",
                                                                     "<root><NoData>Not data is required to execution</NoData></root>",
                                                                     new DatosUsuarioAuditoria
                                                                     {
                                                                         NombreUsuario = "User is not required",
                                                                         CodigoUsuario = "",
                                                                         DireccionIp = red.Ip,

                                                                     });


                string Estado = ConfigurationManager.AppSettings["Estado"];

                OpaServicioFachada FachadaServicio = new OpaServicioFachada();

                //List<Sarc_ViewCreditosParaSeguimiento> EntidadCreditosPersona = new List<Sarc_ViewCreditosParaSeguimiento>();

                //EntidadCreditosPersona = new OpaSarcFachada().BuscarListaCreditosParaSeguimiento();

                //EntidadCreditosPersona = EntidadCreditosPersona.Skip(1335).ToList();


                List<ResultadosScore> EntidadCreditosPersona = new List<ResultadosScore>();

                List<Opa.Sarc.Entidades.ResultadosScore> EntidadResultado = new Opa.Sarc.Negocio.Logica.ResultadosScoreBL().BuscarListarResultadosScore();


                EntidadCreditosPersona = (from c in EntidadResultado
                                          where c.ScoreInicial = true
                                          select c).Distinct().ToList();




                bool CargarHistoria = false;
                //bool CargarHistoria = true;  
                OpaTransaccionalFachada FachadaTransaccional = new OpaTransaccionalFachada();
                List<ResultadosScore> List = new List<ResultadosScore>();
                //new Opa.Sarc.Negocio.FlujoTrabajo.ModeloCalificacionWF2().CargarDatos();


                //foreach (Sarc_ViewCreditosParaSeguimiento Registro in EntidadCreditosPersona)
                //{
                //    string NroSolicitud = Registro.NroSolicitudOtorgamiento == 0 ? "" : Registro.NroSolicitudOtorgamiento.ToString();
                //    decimal CedulaPersona = Convert.ToDecimal(Registro.IdentificacionPersona);
                //    try
                //    {
                //        ArrayList ArrayCentral = new ArrayList();
                //        List<ModeloCalificacion> EntidadModeloCalificacion = new Opa.Sarc.Negocio.FlujoTrabajo.ModeloCalificacionWF().ModeloCalificacionAsociado(CedulaPersona, NroSolicitud, 1, ArrayCentral, CargarHistoria, false, false);

                //        if (EntidadModeloCalificacion != null && EntidadModeloCalificacion.Count > 0)
                //        {
                //            CapacidadPago(CedulaPersona, EntidadModeloCalificacion.FirstOrDefault().NroSolicitud, EntidadModeloCalificacion.FirstOrDefault(), EntidadModeloCalificacion.FirstOrDefault().EgresosExternos);
                //        }

                //        decimal NroSol = Convert.ToDecimal(EntidadModeloCalificacion.FirstOrDefault().NroSolicitud);
                //        bool existe = new OpaTransaccionalFachada().BuscarLiquidaCreditosPersonaPorSolicitud(NroSol);
                //        if (!existe)
                //        {
                //            CreditosPersona CreditoPersona = new OpaSarcFachada().BuscarUnCreditosPersonaPorPagare(Convert.ToInt32(Registro.NroPagare));
                //            string numeroPagare = CreditoPersona.NumeroPagare.ToString();
                //            decimal nroSolicitud = FachadaTransaccional.BuscarLiquidaCreditosPagare(numeroPagare);

                //            FachadaTransaccional.GuardarLiquidaCreditosPersona(new LiquidacreditosPersonas()
                //            {
                //                CedulaPersona = Convert.ToInt64(CedulaPersona),
                //                NumeroSolicitud = nroSolicitud,
                //                Plazo = CreditoPersona.Plazo.ToString(),
                //                coddestino = CreditoPersona.Coddestino,
                //                NumeroSolicitudOtorgamiento = Convert.ToDecimal(EntidadModeloCalificacion.FirstOrDefault().NroSolicitud),
                //                Valor = CreditoPersona.CapitalInicial,
                //                Fecha = CreditoPersona.FechaCargoPrestamo,
                //                Codlinea = CreditoPersona.Codlinea,
                //                Obligatorio = false,
                //                Reestructuracion = false,
                //                CreditosReestructurados = "",
                //                ExigeCodeudor = false,
                //                Codeudores = "",
                //                IncluyeExtras = false,
                //                CuotasExtras = "",
                //                IncluyeCostos = false,
                //                Tasa = CreditoPersona.TasaColocacion,
                //                CostosAdicionales = "",
                //                EsOtorgamientoMasivo = true

                //            });
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        string exe = ex.Message.ToString();

                //    }
                //}
            }
            catch (Exception ex)
            {
                DatosUsuarioAuditoria d = new DatosUsuarioAuditoria()
                {
                    NombreUsuario = "User is not required",
                    CodigoUsuario = "",
                    DireccionIp = red.Ip,
                };

                // Loggin error 
                Guid idlog = OpaExceptionHandling.Handle(ex, d);
                this.StatusTask = Status.Error;
                ValorEnum = (short)StatusTask;
                Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");
                return;
            }

            this.StatusTask = Status.Ok;
            ValorEnum = (short)StatusTask;
            Fa.GuardarEstadoDelServicio(ValorEnum, nombre, "Sarc");
            Task.Factory.StartNew(() => CalificacionCartera());
        }


        public List<ResultadosScore> TransformarResultadoScoreNuevoSeguimiento(List<ResultadoScorePartial> lista, List<ResultadosScore> ListaScoreCapacidades)
        {
            List<ResultadosScore> RestultadoScore = new List<ResultadosScore>();
            RestultadoScore = (from c in lista
                               join d in ListaScoreCapacidades on c.IdResultadosScore equals d.IdResultadosScore
                               select new ResultadosScore
                               {
                                   IdResultadosScore = c.IdResultadosScore,
                                   CedulaPersona = c.CedulaPersona,
                                   NombrePersona = c.NombrePersona,
                                   NroSolicitud = c.NroSolicitud,
                                   FechaScore = c.FechaScore,
                                   PesoCuantitativo = c.PesoCuantitativo,
                                   PesoCualitativo = c.PesoCualitativo,
                                   Calificacion = c.Calificacion,
                                   ResultadosVariables = c.ResultadosVariables,
                                   ResultadosCapacidad = d.ResultadosCapacidad,
                                   Limites = d.Limites,
                                   ScoreInicial = c.ScoreInicial,
                                   IdConsultasCentral = c.IdConsultasCentral,
                                   IdSegmento = c.IdSegmento,

                               }).ToList();
            return RestultadoScore;
        }

        #endregion


    }
}
