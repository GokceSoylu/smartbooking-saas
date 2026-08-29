import { fetchTenantBySlug } from "@/lib/api";
import BookingWidget from "@/components/BookingWidget";
import { notFound } from "next/navigation";

interface Props {
  params: Promise<{ slug: string }>;
}

export default async function TenantBookingPage({ params }: Props) {
  const { slug } = await params;

  let tenant;
  try {
    tenant = await fetchTenantBySlug(slug);
  } catch {
    notFound();
  }

  return (
    <main className="min-h-screen bg-slate-100/60 py-10 px-4 flex flex-col justify-center">
      <BookingWidget tenant={tenant} />
    </main>
  );
}
