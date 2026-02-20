using Repository;
using ScanWorker.Data.Models;
using ScanWorker.Respository;

namespace ScanWorker.Repository;

public class ParcelRepository(ScanWorkerContext context) : Repository<Parcels>(context);

