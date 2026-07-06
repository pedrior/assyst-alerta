using Assyst.Alerta.Models;

namespace Assyst.Alerta.Processing;

internal static class EventDepartments
{
    public static string GetName(Department department) => department switch
    {
        Department.N1 => "1º Nível",
        Department.N2JoaoPessoa => "2º Nível João Pessoa",
        Department.N2CampinaGrande => "2º Nível Campina Grande",
        Department.N2Patos => "2º Nível Patos",
        Department.N2Sousa => "2º Nível Sousa",
        Department.N2ManutencaoEquipamento => "2º Nível Manutenção",
        Department.N2PJe => "2º Nível PJe",
        Department.N2SuporteEspecializado => "2º Nível Especializado",
        Department.N3RedeConectividade => "3º Nível Redes",
        Department.N3Seguranca => "3º Nível Segurança",
        Department.N3SustentacaoInfra => "3º Nível Sustentação",
        Department.N3BusinessIntelligenceBd => "3º Nível BI",
        _ => "Desconhecido"
    };
}