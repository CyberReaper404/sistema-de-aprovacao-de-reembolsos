import React from "react";
import { Document, Page, StyleSheet, Text, View, pdf } from "@react-pdf/renderer";
import { RequestStatus, requestStatusLabels } from "@/types/domain";

interface ProtocolPdfInput {
  requestNumber: string;
  ownerName: string;
  categoryName: string;
  amount: number;
  currency: string;
  expenseDate: string;
  description: string;
  status: RequestStatus;
  costCenterCode: string;
  issuedAt: string;
  decisionReasonLabel?: string;
  decisionComment?: string;
}

const styles = StyleSheet.create({
  page: {
    backgroundColor: "#f0ede8",
    padding: 42,
    fontSize: 10,
    fontFamily: "Helvetica",
    color: "#0f1111"
  },
  sheet: {
    backgroundColor: "#ffffff",
    padding: 24,
    borderWidth: 1,
    borderColor: "#d6d1ca",
    minHeight: 700
  },
  topRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    marginBottom: 28
  },
  identityBlock: {
    gap: 5
  },
  identityTitle: {
    fontSize: 18,
    fontWeight: 600
  },
  identityMuted: {
    color: "#4a4f52",
    lineHeight: 1.4
  },
  invoiceTitle: {
    fontSize: 28,
    fontWeight: 700,
    textTransform: "uppercase",
    marginBottom: 10,
    textAlign: "right"
  },
  protocolLabel: {
    textAlign: "right",
    fontSize: 14,
    fontWeight: 600
  },
  metaRow: {
    flexDirection: "row",
    gap: 28,
    marginBottom: 28
  },
  metaCol: {
    flex: 1,
    gap: 6
  },
  label: {
    fontSize: 8,
    textTransform: "uppercase",
    color: "#6e7275"
  },
  value: {
    fontSize: 10,
    lineHeight: 1.5
  },
  sectionTitle: {
    fontSize: 14,
    fontWeight: 600,
    marginBottom: 12
  },
  lineItemHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    paddingBottom: 8,
    borderBottomWidth: 1,
    borderBottomColor: "#d9d5cf",
    marginBottom: 10
  },
  lineItemRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    paddingBottom: 8,
    marginBottom: 6
  },
  summaryBox: {
    marginLeft: "auto",
    width: 180,
    marginTop: 12,
    marginBottom: 28
  },
  summaryRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    paddingVertical: 4,
    borderBottomWidth: 1,
    borderBottomColor: "#e2ddd6"
  },
  summaryRowStrong: {
    fontWeight: 700
  },
  decisionBox: {
    marginTop: 18,
    marginBottom: 24,
    paddingTop: 10,
    borderTopWidth: 1,
    borderTopColor: "#d9d5cf",
    gap: 6
  },
  decisionLead: {
    fontSize: 11,
    fontWeight: 700
  },
  footerRow: {
    flexDirection: "row",
    gap: 32,
    marginTop: 18
  },
  footerCol: {
    flex: 1,
    gap: 5
  },
  footerParagraph: {
    color: "#4a4f52",
    lineHeight: 1.45
  },
  statusNotice: {
    marginTop: 16,
    paddingTop: 10,
    borderTopWidth: 1,
    borderTopColor: "#d9d5cf",
    color: "#1d2224"
  }
});

function money(amount: number, currency: string) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency
  }).format(amount);
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  }).format(new Date(value));
}

function getDecisionTitle(status: RequestStatus) {
  switch (status) {
    case RequestStatus.Paid:
      return "Solicitação paga";
    case RequestStatus.Approved:
      return "Solicitação aprovada";
    case RequestStatus.Rejected:
      return "Solicitação recusada";
    default:
      return "Solicitação registrada";
  }
}

function getDecisionComment(status: RequestStatus, comment?: string) {
  if (comment) {
    return comment;
  }

  switch (status) {
    case RequestStatus.Paid:
      return "A solicitação foi aprovada na análise e já teve o pagamento registrado pelo financeiro.";
    case RequestStatus.Approved:
      return "A solicitação foi aprovada após validação do comprovante, do valor e do enquadramento da despesa.";
    case RequestStatus.Rejected:
      return "A solicitação foi recusada na análise humana por inconsistência com a política interna ou com a documentação apresentada.";
    default:
      return "O protocolo foi emitido para rastreabilidade e conferência operacional.";
  }
}

function getPaymentTerms(status: RequestStatus) {
  if (status === RequestStatus.Paid) {
    return "Pagamento registrado pelo financeiro e disponibilizado para conferência no histórico da solicitação.";
  }

  if (status === RequestStatus.Rejected) {
    return "Solicitação recusada após análise humana. Este protocolo permanece disponível para auditoria e consulta.";
  }

  return "Solicitação aprovada e aguardando liquidação financeira dentro da rotina operacional definida pela empresa.";
}

function getBankBlock(status: RequestStatus) {
  if (status === RequestStatus.Paid) {
    return {
      title: "Dados do pagamento",
      lines: ["Canal", "Liquidação registrada pelo financeiro", "Situação", "Pagamento concluído e auditável"]
    };
  }

  if (status === RequestStatus.Approved) {
    return {
      title: "Previsão de pagamento",
      lines: ["Canal", "Liquidação pelo financeiro", "Prazo", "Até 5 dias úteis após a aprovação"]
    };
  }

  return {
    title: "Resultado da análise",
    lines: ["Situação", "Solicitação recusada", "Observação", "Sem dados bancários por não haver pagamento"]
  };
}

function ProtocolDocument({
  requestNumber,
  ownerName,
  categoryName,
  amount,
  currency,
  expenseDate,
  description,
  status,
  costCenterCode,
  issuedAt,
  decisionReasonLabel,
  decisionComment
}: ProtocolPdfInput) {
  const bankBlock = getBankBlock(status);

  return (
    <Document>
      <Page size="A4" style={styles.page}>
        <View style={styles.sheet}>
          <View style={styles.topRow}>
            <View style={styles.identityBlock}>
              <Text style={styles.identityTitle}>{ownerName}</Text>
              <Text style={styles.identityMuted}>NIO Ticket</Text>
              <Text style={styles.identityMuted}>Sistema interno de reembolsos corporativos</Text>
            </View>

            <View>
              <Text style={styles.invoiceTitle}>Protocolo</Text>
              <Text style={styles.protocolLabel}>#{requestNumber}</Text>
            </View>
          </View>

          <View style={styles.metaRow}>
            <View style={styles.metaCol}>
              <Text style={styles.label}>Colaborador</Text>
              <Text style={styles.value}>{ownerName}</Text>
              <Text style={styles.label}>Categoria</Text>
              <Text style={styles.value}>{categoryName}</Text>
              <Text style={styles.label}>Centro de custo</Text>
              <Text style={styles.value}>{costCenterCode}</Text>
            </View>

            <View style={styles.metaCol}>
              <Text style={styles.label}>Data da despesa</Text>
              <Text style={styles.value}>{formatDate(expenseDate)}</Text>
              <Text style={styles.label}>Emissão do protocolo</Text>
              <Text style={styles.value}>{formatDate(issuedAt)}</Text>
              <Text style={styles.label}>Situação</Text>
              <Text style={styles.value}>{requestStatusLabels[status]}</Text>
            </View>
          </View>

          <Text style={styles.sectionTitle}>Descrição</Text>
          <View style={styles.lineItemHeader}>
            <Text>Item</Text>
            <Text>Valor</Text>
          </View>
          <View style={styles.lineItemRow}>
            <View style={{ width: "72%" }}>
              <Text>{description}</Text>
            </View>
            <Text>{money(amount, currency)}</Text>
          </View>

          <View style={styles.summaryBox}>
            <View style={styles.summaryRow}>
              <Text>Subtotal</Text>
              <Text>{money(amount, currency)}</Text>
            </View>
            <View style={styles.summaryRow}>
              <Text>Taxas</Text>
              <Text>{money(0, currency)}</Text>
            </View>
            <View style={styles.summaryRow}>
              <Text style={styles.summaryRowStrong}>Total</Text>
              <Text style={styles.summaryRowStrong}>{money(amount, currency)}</Text>
            </View>
            <View style={styles.summaryRow}>
              <Text>Data de referência</Text>
              <Text>{formatDate(expenseDate)}</Text>
            </View>
          </View>

          <View style={styles.decisionBox}>
            <Text style={styles.sectionTitle}>Parecer da análise</Text>
            <Text style={styles.decisionLead}>{getDecisionTitle(status)}</Text>
            <Text>{decisionReasonLabel ?? "Motivo registrado internamente"}</Text>
            <Text>{getDecisionComment(status, decisionComment)}</Text>
          </View>

          <View style={styles.footerRow}>
            <View style={styles.footerCol}>
              <Text style={styles.sectionTitle}>{bankBlock.title}</Text>
              {bankBlock.lines.map((line) => (
                <Text key={line} style={styles.footerParagraph}>
                  {line}
                </Text>
              ))}
            </View>

            <View style={styles.footerCol}>
              <Text style={styles.sectionTitle}>Condições</Text>
              <Text style={styles.footerParagraph}>{getPaymentTerms(status)}</Text>
              <Text style={styles.statusNotice}>
                Este documento é emitido pelo NIO Ticket para rastreabilidade interna do protocolo e auditoria operacional.
              </Text>
            </View>
          </View>
        </View>
      </Page>
    </Document>
  );
}

export async function downloadProtocolPdf(input: ProtocolPdfInput) {
  const blob = await pdf(<ProtocolDocument {...input} />).toBlob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `${input.requestNumber.toLowerCase()}-protocolo.pdf`;
  link.click();
  URL.revokeObjectURL(url);
}
