	.text
	.def	@feat.00;
	.scl	3;
	.type	0;
	.endef
	.globl	@feat.00
.set @feat.00, 0
	.file	"main"
	.def	main;
	.scl	2;
	.type	32;
	.endef
	.globl	main
	.p2align	4, 0x90
main:
.seh_proc main
	subq	$32, %rsp
	.seh_stackalloc 32
	.seh_endprologue
	movq	$0, (%rsp)
	movq	$1, 8(%rsp)
	movq	$4, 24(%rsp)
	movq	$1, 16(%rsp)
	jmp	.LBB0_1
	.p2align	4, 0x90
.LBB0_4:
	movq	$1, 8(%rsp)
	incq	%rax
	movq	%rax, 16(%rsp)
.LBB0_1:
	movq	16(%rsp), %rax
	cmpq	$10001, %rax
	jge	.LBB0_6
	movq	$1, (%rsp)
	movq	(%rsp), %rcx
	cmpq	24(%rsp), %rcx
	jg	.LBB0_4
	.p2align	4, 0x90
.LBB0_5:
	movq	8(%rsp), %rdx
	imulq	(%rsp), %rdx
	movq	%rdx, 8(%rsp)
	incq	%rcx
	movq	%rcx, (%rsp)
	movq	(%rsp), %rcx
	cmpq	24(%rsp), %rcx
	jle	.LBB0_5
	jmp	.LBB0_4
.LBB0_6:
	xorl	%eax, %eax
	addq	$32, %rsp
	retq
	.seh_endproc

