using Opa.Framework.Integration.Jobs;
using Opa.Framework.Integration.Jobs.Tasks;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Text
{
    class Program
    {
        static void Main(string[] args)
        {


            //CargarSeguimientos c = new CargarSeguimientos();
            
            
            
            //c.CargarDatosGeneralPersonasAsync();
            //c.CargarCreditosPersonaAsync();
            //c.CargarPerdidaEsperadaAsync();


            //CargarSeguimientos c = new CargarSeguimientos();
            //c.CargarPerdidaEsperadaAsync();

            //CargarObligaciones c = new CargarObligaciones();
            //c.CargarTemporalObligacionesAsync();

            //CargarSeguimientos c = new CargarSeguimientos();
            //c.CargarScoreSeguimientoAsync(false);

            //CargarSeguimientos c = new CargarSeguimientos();
            //c.CargarAnalisisDePortafolioAsync();

            ////CargarSeguimientos c = new CargarSeguimientos();
            ////c.CargarCreditosPersonaAsync();

            //////////CargarSeguimientos c = new CargarSeguimientos();
            //////////c.CargarCreditosPersonaAsync();


            CargarSeguimientos c = new CargarSeguimientos();

            CargarObligaciones co = new CargarObligaciones();

            CargarOtorgamientoMasivo ot = new CargarOtorgamientoMasivo();

            CargarCalificacionCartera cal = new CargarCalificacionCartera();
           
            //c.SeguimientoprovisionPersonaAsync();

            //c.CargarPerdidaEsperadaAsync();
            //c.CargarCreditosPersonaAsync();

            //c.CargarScoreSeguimientoAsync();
            //c.CalificacionCartera();

            //cal.CargarScoreSeguimientoAsync();
            cal.CalificacionCartera();
            //co.CargarTemporalObligacionesAsync();


            //ot.CargarScoreSeguimientoAsync(true);

            //c.CargarDatosGeneralPersonasAsync();
            //co.CargarTemporalDatosPersonasAsync();
        }
    }
}
