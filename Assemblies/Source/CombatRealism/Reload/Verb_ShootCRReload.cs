using Combat_Realism;
using Verse;

namespace Combat_Realism
{
    public class Verb_ShootCRReload : Verb_ShootCR
    {
        private bool done;
        private CompReloader compAmmo;

        protected override bool TryCastShot()
        {
            if ( !done )
            {
                compAmmo = ownerEquipment.GetComp< CompReloader >();
                done = true;
            }

            if ( compAmmo == null )
            {
                Log.ErrorOnce( "No compAmmo found!", 12423 );
                return base.TryCastShot();
            }

            if ( compAmmo.needReload || compAmmo.count <= 0)
            {
                compAmmo.StartReload();
                return false;
            }
            
            if ( !base.TryCastShot() )
            {
                return false;
            }

            compAmmo.count--;
            if ( compAmmo.count <= 0 )
            {
                compAmmo.StartReload();
            }

            return true;
        }

        public override void Notify_Dropped()
        {
            base.Notify_Dropped();
            caster = null;
        }
    }
}
