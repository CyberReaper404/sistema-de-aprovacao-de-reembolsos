interface PlaceholderStateProps {
  eyebrow?: string;
  title: string;
  description: string;
}

export function PlaceholderState({ eyebrow, title, description }: PlaceholderStateProps) {
  return (
    <div className="placeholder-state">
      {eyebrow ? <span className="placeholder-state__eyebrow">{eyebrow}</span> : null}
      <h1>{title}</h1>
      <p>{description}</p>
    </div>
  );
}
