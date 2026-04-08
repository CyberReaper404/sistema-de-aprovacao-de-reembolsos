import type { ReactNode } from "react";

interface TableColumn {
  key: string;
  label: string;
  align?: "left" | "right";
}

interface TablePreviewProps {
  columns: TableColumn[];
  rows: Record<string, ReactNode>[];
}

export function TablePreview({ columns, rows }: TablePreviewProps) {
  return (
    <div className="table-preview">
      <table>
        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column.key} className={column.align === "right" ? "table-preview__cell--right" : undefined}>
                {column.label}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, rowIndex) => (
            <tr key={rowIndex}>
              {columns.map((column) => (
                <td key={column.key} className={column.align === "right" ? "table-preview__cell--right" : undefined}>
                  {row[column.key]}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
