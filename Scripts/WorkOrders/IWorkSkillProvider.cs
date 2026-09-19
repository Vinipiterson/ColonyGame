/*
 * Implemented by whatever Node hosts a Worker component
 * (Colonist today, potentially other actors later) so Worker
 * can ask it "how much do you want this kind of work?" without
 * Worker needing to know anything about skills, personality,
 * or needs itself.
 */
public interface IWorkSkillProvider
{
    int GetPriorityModifier(WorkOrderType type);
}