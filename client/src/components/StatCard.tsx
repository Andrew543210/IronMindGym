import type { ReactNode } from 'react';

interface StatCardProps {
  label: string;
  value: ReactNode;
  unit?: string;
  note?: ReactNode;
  children?: ReactNode;
}

export function StatCard({ label, value, unit, note, children }: StatCardProps) {
  return (
    <div className="card stat-card h-100"><div className="card-body">
      <div className="stat-label">{label}</div>
      <div className="stat-value">{value}{unit && <small> {unit}</small>}</div>
      {children}
      {note && <div className="stat-note">{note}</div>}
    </div></div>
  );
}
