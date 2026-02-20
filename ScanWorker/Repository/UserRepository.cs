using Repository;
using ScanWorker.Data.Models;
using ScanWorker.Respository;

namespace ScanWorker.Repository;

public class UserRepository(ScanWorkerContext context) : Repository<User>(context);

