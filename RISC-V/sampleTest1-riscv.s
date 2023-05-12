	.text
	.attribute	4, 16
	.attribute	5, "rv64i2p0"
	.file	"main"
	.globl	start                           # -- Begin function start
	.p2align	2
	.type	start,@function
start:                                  # @start
	.cfi_startproc
# %bb.0:                                # %entry
	addi	sp, sp, -64
	.cfi_def_cfa_offset 64
	sd	ra, 56(sp)                      # 8-byte Folded Spill
	sd	s0, 48(sp)                      # 8-byte Folded Spill
	.cfi_offset ra, -8
	.cfi_offset s0, -16
	li	a0, 1
	sd	a0, 40(sp)
	li	a0, 20
	sd	a0, 32(sp)
	li	a0, 9
	sd	a0, 8(sp)
	li	a0, 9
	call	write@plt
	ld	a0, 40(sp)
.LBB0_1:                                # %for.condition
                                        # =>This Inner Loop Header: Depth=1
	sd	a0, 24(sp)
	ld	a0, 24(sp)
	ld	a1, 32(sp)
	bge	a0, a1, .LBB0_3
# %bb.2:                                # %for.body
                                        #   in Loop: Header=BB0_1 Depth=1
	ld	a0, 24(sp)
	ld	a1, 8(sp)
	slli	a0, a0, 1
	add	a0, a1, a0
	add	a0, a0, a1
	sd	a0, 0(sp)
	call	write@plt
	ld	a0, 24(sp)
	addi	a0, a0, 1
	j	.LBB0_1
.LBB0_3:                                # %exit
	sd	zero, 16(sp)
.LBB0_4:                                # %while.cond
                                        # =>This Inner Loop Header: Depth=1
	ld	s0, 16(sp)
	ld	a0, 32(sp)
	bge	s0, a0, .LBB0_6
# %bb.5:                                # %while.body
                                        #   in Loop: Header=BB0_4 Depth=1
	ld	a0, 16(sp)
	ld	a1, 40(sp)
	ld	a2, 8(sp)
	add	a0, a0, a1
	add	a0, a0, a2
	sd	a0, 16(sp)
	call	write@plt
	addi	a0, s0, 1
	sd	a0, 16(sp)
	j	.LBB0_4
.LBB0_6:                                # %while.end
	li	a0, 0
	ld	ra, 56(sp)                      # 8-byte Folded Reload
	ld	s0, 48(sp)                      # 8-byte Folded Reload
	addi	sp, sp, 64
	ret
.Lfunc_end0:
	.size	start, .Lfunc_end0-start
	.cfi_endproc
                                        # -- End function
	.section	".note.GNU-stack","",@progbits
