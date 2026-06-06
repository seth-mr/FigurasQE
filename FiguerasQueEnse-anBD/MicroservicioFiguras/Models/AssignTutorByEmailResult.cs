namespace MicroservicioFiguras.Models;

public enum AssignTutorByEmailResult
{
    Success,
    StudentNotFound,
    StudentEmailBelongsToTutor,
    TutorNotFound,
    StudentAlreadyAssignedToCurrentTutor,
    StudentAlreadyAssignedToAnotherTutor
}
